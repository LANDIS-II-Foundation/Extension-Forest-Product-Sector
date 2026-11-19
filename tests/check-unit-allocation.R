#!/usr/bin/env Rscript
## Checks that FPSM sends each management unit's harvest to that unit's mills.
##
## Usage: Rscript tests/check-unit-allocation.R <dir with FPSM's output> <case dir> <mills csv>
##
## The case directory holds units.csv (row, column, unit: the management unit of each cell,
## the same values as the case's map) and the mills csv (unit, column, mill: the mill that
## receives all of a unit's carbon from one ForCS flux-log column, as the case's
## ProportionsFromForestToMills rows send it). For each year and mill, the carbon in step 1
## of FPS_test_out.csv must equal the carbon in the flux logs of the cells sent to that mill,
## converted from g/m2 to tonnes per cell with the configuration's CellLength.
##
## Prints the totals by year and mill, and exits with status 1 if any differ. Base R only.

args <- commandArgs(trailingOnly = TRUE)
if (length(args) != 3) {
  stop(
    "usage: Rscript tests/check-unit-allocation.R <output dir> <case dir> <mills csv>"
  )
}
work <- args[[1]]
case_dir <- args[[2]]

units <- utils::read.csv(file.path(case_dir, "units.csv"), strip.white = TRUE)
mills <- utils::read.csv(file.path(case_dir, args[[3]]), strip.white = TRUE)

## CellLength (m) converts g/m2 to tonnes per cell: x CellLength^2 x 1e-6.
config <- readLines(file.path(work, "fps.txt"))
cell_length <- as.numeric(sub(
  "^CellLength\\s+",
  "",
  grep("^CellLength\\s", config, value = TRUE)
))
to_tonnes <- max(cell_length, 1)^2 * 1e-6

## The carbon each flux-log record sends to FPSM, one row per record and column.
harvest <- NULL
for (log in c("log_FluxBio.csv", "log_FluxDOM.csv")) {
  ## ForCS ends each log_FluxDOM.csv row with a comma, so rows have one field more
  ## than the header; the fields are named from the header in order.
  header <- trimws(strsplit(readLines(file.path(work, log), n = 1), ",")[[1]])
  flux <- utils::read.csv(
    file.path(work, log),
    header = FALSE,
    skip = 1,
    strip.white = TRUE
  )[seq_along(header)]
  names(flux) <- header
  for (column in intersect(unique(mills$column), names(flux))) {
    harvest <- rbind(
      harvest,
      data.frame(
        year = flux$Time,
        row = flux$row,
        col = flux$column,
        column = column,
        carbon = flux[[column]] * to_tonnes
      )
    )
  }
}
harvest <- harvest[harvest$carbon > 0, ]
harvest$unit <- units$unit[match(
  paste(harvest$row, harvest$col),
  paste(units$row, units$column)
)]
harvest$mill <- mills$mill[match(
  paste(harvest$unit, harvest$column),
  paste(mills$unit, mills$column)
)]
if (anyNA(harvest$mill)) {
  stop(
    "units.csv or the mills csv does not cover every cell and column with carbon"
  )
}
expected <- stats::aggregate(carbon ~ year + mill, data = harvest, FUN = sum)

## FPSM writes the carbon each mill receives as step 1 rows of FPS_test_out.csv, with
## market -99; codes of 1000 and above there are disposal pools, not mills.
test_out <- utils::read.csv(
  file.path(work, "FPS_test_out.csv"),
  col.names = c("step", "year", "market", "pool", "amount"),
  strip.white = TRUE
)
reported <- test_out[test_out$step == 1 & test_out$pool < 1000, ]
reported <- stats::aggregate(amount ~ year + pool, data = reported, FUN = sum)
names(reported) <- c("year", "mill", "reported")

both <- merge(expected, reported, by = c("year", "mill"), all = TRUE)
both$carbon[is.na(both$carbon)] <- 0
both$reported[is.na(both$reported)] <- 0
both$diff <- both$reported - both$carbon
print(round(both[order(both$year, both$mill), ], 6), row.names = FALSE)

bad <- abs(both$diff) > 1e-6 + 1e-9 * pmax(abs(both$carbon), abs(both$reported))
if (any(bad)) {
  cat(
    sprintf(
      "FAIL: year %d, mill %d receives %.6f t, but its units' harvest is %.6f t\n",
      both$year[bad],
      both$mill[bad],
      both$reported[bad],
      both$carbon[bad]
    ),
    sep = ""
  )
  quit(status = 1)
}
