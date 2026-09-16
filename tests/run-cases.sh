#!/usr/bin/env bash
#
# Runs each configuration in tests/cases/ through FPSM inside the LANDIS-II
# runtime image and checks it behaves as that case's `expect` file records.
#
# Each case directory holds:
#   fps.txt  a configuration, normally the SimpleTier1 example with one token
#            changed, so that the case isolates a single behaviour
#   expect   what that configuration should do, as key: value lines
#   log_Flux*.csv  optional; overrides the SimpleTier1 input of the same name,
#            for cases the shipped inputs cannot exercise
#
# Supported expectations:
#   exit_zero: yes|no          FPSM exits 0, or does not
#   stdout_contains: <text>    text appears in FPSM's combined output
#   log_contains: <text>       text appears in FPS_log.txt
#   log_empty: yes             FPS_log.txt is empty
#   matches_example: <name>    FPS_raw_out.csv is byte-identical to the output
#                              committed under deploy/examples/<name>/
#   matches_recorded: <file>   FPS_raw_out.csv is byte-identical to <file> in
#                              the case directory
#
# Usage: tests/run-cases.sh <build-output-dir> <landis-image>

set -uo pipefail

BUILD_DIR=${1:?usage: tests/run-cases.sh <build-output-dir> <landis-image>}
IMAGE=${2:?usage: tests/run-cases.sh <build-output-dir> <landis-image>}

ROOT=$(cd "$(dirname "$0")/.." && pwd)
INPUTS="$ROOT/deploy/examples/SimpleTier1Ex"

status=0

for case_dir in "$ROOT"/tests/cases/*/; do
  name=$(basename "$case_dir")
  echo "::group::$name"

  work=$(mktemp -d)
  cp "$INPUTS"/log_Flux*.csv "$work/"
  #  A case may ship its own flux logs where the example inputs cannot
  #  exercise the behaviour under test.
  for override in "$case_dir"log_Flux*.csv; do
    [ -e "$override" ] && cp "$override" "$work/"
  done
  cp "$case_dir/fps.txt" "$work/"
  cp "$BUILD_DIR"/*.dll "$BUILD_DIR"/*.json "$work/"

  out=$(docker run --rm -v "$work:/work" -w /work "$IMAGE" bash -c '
    core=$(find /opt -name Landis.Core.dll -print -quit 2>/dev/null)
    cp -n "$(dirname "$core")"/*.dll /work/ 2>/dev/null || true
    dotnet /work/Landis.Extension.FPS-v1.dll fps.txt
  ' 2>&1)
  code=$?
  echo "exit code: $code"
  echo "$out" | sed 's/^/  | /'

  fail() { echo "::error::$name: $1"; status=1; }

  while IFS= read -r line; do
    [ -z "$line" ] && continue
    case "$line" in \#*) continue ;; esac
    key=${line%%:*}
    val=${line#*: }
    case "$key" in
      exit_zero)
        if [ "$val" = yes ] && [ $code -ne 0 ]; then fail "expected exit 0, got $code"; fi
        if [ "$val" = no  ] && [ $code -eq 0 ]; then fail "expected a non-zero exit, got 0"; fi
        ;;
      stdout_contains)
        echo "$out" | grep -qF -- "$val" || fail "output did not contain: $val" ;;
      log_contains)
        grep -qF -- "$val" "$work/FPS_log.txt" 2>/dev/null || fail "FPS_log.txt did not contain: $val" ;;
      log_empty)
        [ -s "$work/FPS_log.txt" ] && fail "expected an empty FPS_log.txt" ;;
      matches_example)
        if ! diff -q "$ROOT/deploy/examples/$val/FPS_raw_out.csv" "$work/FPS_raw_out.csv" >/dev/null 2>&1; then
          fail "FPS_raw_out.csv differs from deploy/examples/$val"
          diff "$ROOT/deploy/examples/$val/FPS_raw_out.csv" "$work/FPS_raw_out.csv" | head -10
        fi
        ;;
      matches_recorded)
        if ! diff -q "$case_dir/$val" "$work/FPS_raw_out.csv" >/dev/null 2>&1; then
          fail "FPS_raw_out.csv differs from the recorded $val"
          diff "$case_dir/$val" "$work/FPS_raw_out.csv" | head -10
        fi
        ;;
      *) fail "unknown expectation '$key'" ;;
    esac
  done < "$case_dir/expect"

  #  Keep the outputs so a failing run can be inspected, and so a new case's
  #  behaviour can be recorded from CI.
  if [ -n "${CASE_RESULTS:-}" ]; then
    mkdir -p "$CASE_RESULTS/$name"
    cp "$work"/FPS_*.csv "$work"/FPS_log.txt "$CASE_RESULTS/$name/" 2>/dev/null || true
  fi

  rm -rf "$work"
  echo "::endgroup::"
done

exit $status
