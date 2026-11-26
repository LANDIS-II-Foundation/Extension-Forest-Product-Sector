#!/usr/bin/env Rscript
## Checks that every pool of one secondary product retires all its carbon in the year it
## is made, as the "instant" retirement function should.
##
## Usage: Rscript tests/check-first-year-retirement.R <dir with FPSM's output> <product code>
##
## For each year and market in which the product is made (step 4 of FPS_test_out.csv), the
## product's type 5 rows in FPS_raw_out.csv must send the whole amount on in that same year,
## leave 0 in the product's stock at the end of that year, and report no stock after it.
##
## Prints one line per pool, and exits with status 1 if any check fails. Base R only.

## FPS_raw_out.csv type for secondary products.
secondary <- 5

args <- commandArgs(trailingOnly = TRUE)
if (length(args) != 2) {
  stop(
    "usage: Rscript tests/check-first-year-retirement.R <output dir> <product code>"
  )
}
work <- args[[1]]
product <- as.integer(args[[2]])

## Columns are named here rather than taken from the headers, which contain spaces and "/".
test_out <- utils::read.csv(
  file.path(work, "FPS_test_out.csv"),
  col.names = c("step", "year", "market", "pool", "amount"),
  strip.white = TRUE
)
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

made <- test_out[test_out$step == 4 & test_out$pool == product, ]
rows <- raw_out[raw_out$type == secondary & raw_out$from == product, ]
## A stock row carries AmountRetained for one pool and year; every other row is a flow.
is_stock <- rows$emitted == 0 & rows$to == rows$from

close <- function(a, b) abs(a - b) <= 1e-6 + 1e-9 * pmax(abs(a), abs(b))

pools <- data.frame(year = made$year, market = made$market, made = made$amount)
in_pool <- function(i) {
  rows$created == pools$year[i] & rows$market == pools$market[i]
}
pools$retired_that_year <- vapply(
  seq_len(nrow(pools)),
  function(i) {
    sum(rows$emitted[in_pool(i) & !is_stock & rows$reported == pools$year[i]])
  },
  numeric(1)
)
pools$stock_end_of_year <- vapply(
  seq_len(nrow(pools)),
  function(i) {
    sum(rows$retained[in_pool(i) & is_stock & rows$reported == pools$year[i]])
  },
  numeric(1)
)
pools$later_stock_rows <- vapply(
  seq_len(nrow(pools)),
  function(i) sum(in_pool(i) & is_stock & rows$reported > pools$year[i]),
  numeric(1)
)

errors <- character()
if (nrow(pools) == 0) {
  errors <- c(errors, sprintf("product %d is never made", product))
}
bad <- !close(pools$retired_that_year, pools$made)
errors <- c(
  errors,
  sprintf(
    "year %d, market %d: %.6f made, but %.6f retired that year",
    pools$year[bad],
    pools$market[bad],
    pools$made[bad],
    pools$retired_that_year[bad]
  )
)
bad <- !close(pools$stock_end_of_year, 0)
errors <- c(
  errors,
  sprintf(
    "year %d, market %d: %.6f still in stock at the end of the year it was made",
    pools$year[bad],
    pools$market[bad],
    pools$stock_end_of_year[bad]
  )
)
bad <- pools$later_stock_rows > 0
errors <- c(
  errors,
  sprintf(
    "year %d, market %d: stock reported in %d later years",
    pools$year[bad],
    pools$market[bad],
    pools$later_stock_rows[bad]
  )
)

print(pools, row.names = FALSE)
if (length(errors) > 0) {
  cat(sprintf("FAIL: %s\n", errors), sep = "")
  quit(status = 1)
}
