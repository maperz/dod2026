#!/usr/bin/env python3
"""Normalize a wide S&P dataset to DATE,TICKER,CLOSE_PRICE rows."""

from __future__ import annotations

import csv
from collections import Counter
from datetime import datetime
from pathlib import Path


DEFAULT_INPUT = Path("snp_daily_dataset.csv")
DEFAULT_OUTPUT = Path("snp_daily_dataset_iso_transposed.csv")
DATE_FORMATS = ("%m/%d/%y", "%m/%d/%Y", "%Y-%m-%d")
OUTPUT_HEADER = ["DATE", "TICKER", "CLOSE_PRICE"]


def iso_date_string(value: str) -> str:
    """Return a YYYY-MM-DD date string for any supported input date."""
    for date_format in DATE_FORMATS:
        try:
            return datetime.strptime(value, date_format).date().isoformat()
        except ValueError:
            pass

    raise ValueError(f"Unsupported date format: {value}")


def close_price_columns(header: list[str]) -> list[tuple[int, str]]:
    """Return the first occurrence of each ticker column, preserving header order."""
    if not header or header[0] != "Date":
        raise ValueError("Expected the first CSV column to be named 'Date'.")

    seen_tickers: set[str] = set()
    columns: list[tuple[int, str]] = []

    for index, ticker in enumerate(header[1:], start=1):
        if not ticker:
            continue

        if ticker in seen_tickers:
            continue

        seen_tickers.add(ticker)
        columns.append((index, ticker))

    if not columns:
        raise ValueError("No ticker columns found in input CSV.")

    return columns


def normalize_snp_data(
    input_path: Path,
    output_path: Path,
    include_empty_prices: bool,
) -> tuple[int, int, int, list[tuple[str, int]]]:
    output_path.parent.mkdir(parents=True, exist_ok=True)
    date_counts: Counter[str] = Counter()
    rows_read = 0
    rows_written = 0
    duplicate_pairs = 0
    seen_pairs: set[tuple[str, str]] = set()

    with input_path.open("r", newline="", encoding="utf-8-sig") as input_file:
        reader = csv.reader(input_file)
        try:
            header = next(reader)
        except StopIteration as exc:
            raise ValueError(f"Input CSV is empty: {input_path}") from exc

        columns = close_price_columns(header)

        with output_path.open("w", newline="", encoding="utf-8") as output_file:
            writer = csv.writer(output_file)
            writer.writerow(OUTPUT_HEADER)

            for row in reader:
                if not row:
                    continue

                rows_read += 1
                date = iso_date_string(row[0])
                date_counts[date] += 1

                for index, ticker in columns:
                    close_price = row[index] if index < len(row) else ""
                    if not include_empty_prices and close_price == "":
                        continue

                    pair = (date, ticker)
                    if pair in seen_pairs:
                        duplicate_pairs += 1
                        continue

                    seen_pairs.add(pair)
                    writer.writerow([date, ticker, close_price])
                    rows_written += 1

    duplicate_dates = sorted((date, count) for date, count in date_counts.items() if count > 1)
    return rows_read, rows_written, duplicate_pairs, duplicate_dates


def prompt_for_path(prompt: str, default: Path) -> Path:
    response = input(f"{prompt} [{default}]: ").strip()
    return Path(response) if response else default


def prompt_for_yes_no(prompt: str, default: bool = False) -> bool:
    suffix = "Y/n" if default else "y/N"

    while True:
        response = input(f"{prompt} [{suffix}]: ").strip().lower()
        if not response:
            return default
        if response in {"y", "yes"}:
            return True
        if response in {"n", "no"}:
            return False

        print("Please answer yes or no.")


def prompt_for_options() -> tuple[Path, Path, bool]:
    input_path = prompt_for_path("Input CSV file", DEFAULT_INPUT)
    output_path = prompt_for_path("Output CSV file", DEFAULT_OUTPUT)
    include_empty_prices = prompt_for_yes_no("Include empty close-price rows")
    return input_path, output_path, include_empty_prices


def main() -> None:
    input_path, output_path, include_empty_prices = prompt_for_options()
    rows_read, rows_written, duplicate_pairs, duplicate_dates = normalize_snp_data(
        input_path,
        output_path,
        include_empty_prices,
    )

    print(f"Read {rows_read} daily row(s) from {input_path}")
    print(f"Wrote {rows_written} DATE,TICKER,CLOSE_PRICE row(s) to {output_path}")
    print(f"Skipped {duplicate_pairs} duplicate DATE/TICKER pair(s)")

    if duplicate_dates:
        print(f"Found {len(duplicate_dates)} duplicate date(s):")
        for date, count in duplicate_dates:
            print(f"{date}: {count}")
    else:
        print("Found 0 duplicate dates.")


if __name__ == "__main__":
    main()
