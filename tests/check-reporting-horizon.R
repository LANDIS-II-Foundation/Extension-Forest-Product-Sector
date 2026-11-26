#!/usr/bin/env Rscript
## Checks that FPSM reports every pool through the final year, and nothing after it.
##
## Usage: Rscript tests/check-reporting-horizon.R <dir with FPS_raw_out.csv> <final year>
##
## The final year is the last harvest year plus YearsPostHarvest. In FPS_raw_out.csv a pool's
## stock is the AmountRetained of its type 5 rows (secondary products) or type 4 rows
## (disposal pools); a pool is one (Type, YearCreated, Market, FromPool). Every pool must
## report every year from its first to its last with no gap, and every pool that still holds
## carbon at its last row must report the final year. No row of any type may be dated after
## the final year, and no gas or transfer row (types 1, 2 and 3) may carry stock.
##
## Prints a summary, and exits with status 1 if any check fails. Base R only.

## FPS_raw_out.csv types that carry stock: disposal pools and secondary products.
stock_types <- c(4, 5)

args <- commandArgs(trailingOnly = TRUE)
if (length(args) != 2) {
  stop(
    "usage: Rscript tests/check-reporting-horizon.R <dir with FPSM's output> <final year>"
  )
}
work <- args[[1]]
final_year <- as.integer(args[[2]])

## Columns are named here rather than taken from the headers, which contain spaces and "/".
raw_out <- utils::read.csv(
  file.path(work, "FPS_raw_out.csv"),
  col.names = c(
    "type",
    "created",
    "reported",
    "market",
    "from",
    "to",
    "emitted",
    "retained"
  ),
  strip.white = TRUE
)

## A stock row carries AmountRetained for one pool and year; every other row is a flow.
is_stock <- raw_out$type %in%
  stock_types &
  raw_out$emitted == 0 &
  raw_out$to == raw_out$from
stock <- raw_out[is_stock, ]
pool <- paste(stock$type, stock$created, stock$market, stock$from)

## One row per pool: its type, first and last year reported, number of rows, and the
## carbon in its last row.
in_order <- order(pool, stock$reported)
last_rows <- in_order[!duplicated(pool[in_order], fromLast = TRUE)]
pools <- data.frame(
  pool = pool[last_rows],
  type = stock$type[last_rows],
  last = stock$reported[last_rows],
  carbon_at_last = stock$retained[last_rows]
)
pools$first <- as.vector(tapply(stock$reported, pool, min)[pools$pool])
pools$rows <- as.vector(table(pool)[pools$pool])

errors <- character()
if (nrow(pools) == 0) {
  errors <- c(errors, "FPS_raw_out.csv has no stock rows")
}
late <- raw_out$reported > final_year
if (any(late)) {
  errors <- c(
    errors,
    sprintf(
      "%d rows are dated after the final year, %d",
      sum(late),
      final_year
    )
  )
}
gappy <- pools$rows != pools$last - pools$first + 1
if (any(gappy)) {
  errors <- c(
    errors,
    sprintf(
      "%d pools skip or repeat a year; e.g. (Type YearCreated Market Pool) %s",
      sum(gappy),
      pools$pool[gappy][1]
    )
  )
}
short <- pools$carbon_at_last > 0 & pools$last < final_year
if (any(short)) {
  errors <- c(
    errors,
    sprintf(
      "%d pools still hold carbon but stop before the final year, %d; e.g. %s stops in %d",
      sum(short),
      final_year,
      pools$pool[short][1],
      pools$last[short][1]
    )
  )
}
stock_elsewhere <- !raw_out$type %in% stock_types & raw_out$retained != 0
if (any(stock_elsewhere)) {
  errors <- c(
    errors,
    sprintf(
      "%d gas or transfer rows (types 1-3) carry AmountRetained",
      sum(stock_elsewhere)
    )
  )
}

summary <- data.frame(type = sort(unique(pools$type)))
summary$pools <- vapply(
  summary$type,
  function(t) sum(pools$type == t),
  integer(1)
)
summary$reach_final_year <- vapply(
  summary$type,
  function(t) sum(pools$type == t & pools$last == final_year),
  integer(1)
)
cat(sprintf("final year %d; pools by FPS_raw_out.csv type:\n", final_year))
print(summary, row.names = FALSE)
if (length(errors) > 0) {
  cat(sprintf("FAIL: %s\n", errors), sep = "")
  quit(status = 1)
}
