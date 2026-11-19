#!/usr/bin/env Rscript
## Checks that FPSM's secondary-product stock accounts for every tonne of carbon.
##
## Usage: Rscript tests/check-product-balance.R <dir with FPS_test_out.csv and FPS_raw_out.csv>
##
## For every year t that FPS_raw_out.csv reports, the carbon in secondary products at the end
## of t (AmountRetained of the type 5 rows reported in t) must equal the carbon made into
## secondary products up to t (step 4 of FPS_test_out.csv) less the carbon those products sent
## to disposal pools (codes 1000 and above) up to t. Carbon recycled from one product to
## another stays in products, so it is in neither side's subtraction: missing recycled carbon
## makes the stock too small, and recycled carbon counted twice makes it too large.
##
## It also checks the rows that report recycled carbon: in each year they hold exactly the
## carbon recycled that year, and no stock row appears twice.
##
## Prints the balance by year, and exits with status 1 if any check fails. Base R only.

## Codes of 1000 and above are FPSM's fixed disposal pools and gases (user guide, Table 1);
## user-defined products are below 1000.
first_special_code <- 1000
## FPS_test_out.csv writes special pools in step 4 with this market code.
no_market <- -99
## FPS_raw_out.csv type for secondary products.
secondary <- 5

args <- commandArgs(trailingOnly = TRUE)
if (length(args) != 1) {
  stop("usage: Rscript tests/check-product-balance.R <dir with FPSM's output>")
}
work <- args[[1]]

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

made <- test_out[
  test_out$step == 4 &
    test_out$market != no_market &
    test_out$pool < first_special_code,
]
products <- raw_out[raw_out$type == secondary, ]

## A stock row carries AmountRetained for one pool and year; every other row is a flow.
is_stock <- products$emitted == 0 & products$to == products$from
## Recycled carbon is reported in the year before the year of the pool it went into.
is_recycled_stock <- products$created == products$reported + 1

years <- sort(unique(products$reported))
sum_by_year <- function(values, keep) {
  vapply(years, function(t) sum(values[keep(t)]), numeric(1))
}

balance <- data.frame(year = years)
balance$made <- vapply(
  years,
  function(t) sum(made$amount[made$year <= t]),
  numeric(1)
)
balance$disposed <- sum_by_year(products$emitted, function(t) {
  products$reported <= t & products$to >= first_special_code
})
balance$recycled <- sum_by_year(products$emitted, function(t) {
  products$reported == t & products$to < first_special_code
})
balance$expected <- balance$made - balance$disposed
balance$reported <- sum_by_year(products$retained, function(t) {
  products$reported == t
})
balance$recycled_rows <- sum_by_year(products$retained, function(t) {
  products$reported == t & is_recycled_stock
})
balance$diff <- balance$reported - balance$expected

close <- function(a, b) abs(a - b) <= 1e-6 + 1e-9 * pmax(abs(a), abs(b))

errors <- character()
if (length(years) == 0) {
  errors <- c(errors, "FPS_raw_out.csv has no secondary product rows")
}
bad <- !close(balance$reported, balance$expected)
errors <- c(
  errors,
  sprintf(
    "year %d: secondary product stock is %.6f, but made less disposed to date is %.6f",
    balance$year[bad],
    balance$reported[bad],
    balance$expected[bad]
  )
)
bad <- !close(balance$recycled_rows, balance$recycled)
errors <- c(
  errors,
  sprintf(
    "year %d: %.6f was recycled, but the rows reporting recycled carbon hold %.6f",
    balance$year[bad],
    balance$recycled[bad],
    balance$recycled_rows[bad]
  )
)
ahead <- products$created > products$reported + 1 |
  (is_recycled_stock & !is_stock)
if (any(ahead)) {
  errors <- c(
    errors,
    sprintf(
      "%d rows are dated after the year they report and are not stock of recycled carbon",
      sum(ahead)
    )
  )
}
keys <- products[is_stock, c("created", "reported", "market", "from")]
if (anyDuplicated(keys)) {
  errors <- c(
    errors,
    sprintf(
      "%d stock rows repeat a (YearCreated, YearReported, Market, Pool)",
      sum(duplicated(keys))
    )
  )
}

print(round(balance, 6), row.names = FALSE)
if (length(errors) > 0) {
  cat(sprintf("FAIL: %s\n", errors), sep = "")
  quit(status = 1)
}
