# List of strategies

See the [overview](./overview.md) for an explanation of what strategies are and
definitions for some of the terms used below.

## Strategy 1

1. [Exact GFD](rules.md#exact-gfd)
2. [Exact All](rules.md#exact-all)
3. [Fuzzy GFD](rules.md#fuzzy-gfd)
4. [Fuzzy All](rules.md#fuzzy-all)
5. [Fuzzy GFD Range](rules.md#fuzzy-gfd-range)
6. [Fuzzy Alt DOB](rules.md#fuzzy-alt-dob)

## Strategy 2

Strategy 2 removes the exact searches from Strategy 1, and uses a large variation of fuzzy and non-fuzzy searches. The order of the queries is not optimized.

1. [Non Fuzzy GFD](rules.md#non-fuzzy-gfd)
2. [Non Fuzzy GFD Range](rules.md#non-fuzzy-gfd-range)
3. [Non Fuzzy All (Postcode Wildcard)](rules.md#non-fuzzy-all-postcode-wildcard)
4. [Non Fuzzy All](rules.md#non-fuzzy-all)
5. [Fuzzy GFD](rules.md#fuzzy-gfd)
6. [Fuzzy GFD Range (Postcode Wildcard)](rules.md#fuzzy-gfd-range-postcode-wildcard)
7. [Fuzzy GFD Range (Postcode)](rules.md#fuzzy-gfd-range-postcode)
8. [Fuzzy All](rules.md#fuzzy-all)
9. [Fuzzy Alt DOB](rules.md#fuzzy-alt-dob)

## Strategy 3

Strategy 3 went through many iterations during testing, and the three most recent versions are documented below. For more details, please see the full version history [here](strategy3.md).

### Version 14

1. [Non-Fuzzy GFD](rules.md#non-fuzzy-gfd)
3. [Fuzzy GFD](rules.md#fuzzy-gfd)
4. [Fuzzy All](rules.md#fuzzy-all) 
5. [Non-Fuzzy GFD Range](rules.md#non-fuzzy-gfd-range)
5. [Non-Fuzzy GFD Range (Postcode)](rules.md#non-fuzzy-gfd-range-postcode)
6. [Fuzzy GFD Range](rules.md#fuzzy-gfd-range)
7. [Fuzzy GFD Range (Postcode)](rules.md#fuzzy-gfd-range-postcode)

### Version 15

Adds postcode wildcard rules to the end of the search.

1. [Non-Fuzzy GFD](rules.md#non-fuzzy-gfd)
3. [Fuzzy GFD](rules.md#fuzzy-gfd)
4. [Fuzzy All](rules.md#fuzzy-all) 
5. [Non-Fuzzy GFD Range](rules.md#non-fuzzy-gfd-range)
5. [Non-Fuzzy GFD Range (Postcode)](rules.md#non-fuzzy-gfd-range-postcode)
6. [Fuzzy GFD Range](rules.md#fuzzy-gfd-range)
7. [Fuzzy GFD Range (Postcode)](rules.md#fuzzy-gfd-range-postcode)
8. [Non-Fuzzy GFD Range (Postcode Wildcard)](rules.md#non-fuzzy-gfd-range-postcode-wildcard)
9. [Fuzzy GFD Range (Postcode Wildcard)](rules.md#fuzzy-gfd-range-postcode-wildcard)

### Version 16

Adds a postcode field to the initial rule of V15.

1. [Non-Fuzzy GFD (Postcode)](rules.md#non-fuzzy-gfd-postcode)
3. [Fuzzy GFD](rules.md#fuzzy-gfd)
4. [Fuzzy All](rules.md#fuzzy-all) 
5. [Non-Fuzzy GFD Range](rules.md#non-fuzzy-gfd-range)
5. [Non-Fuzzy GFD Range (Postcode)](rules.md#non-fuzzy-gfd-range-postcode)
6. [Fuzzy GFD Range](rules.md#fuzzy-gfd-range)
7. [Fuzzy GFD Range (Postcode)](rules.md#fuzzy-gfd-range-postcode)
8. [Non-Fuzzy GFD Range (Postcode Wildcard)](rules.md#non-fuzzy-gfd-range-postcode-wildcard)
9. [Fuzzy GFD Range (Postcode Wildcard)](rules.md#fuzzy-gfd-range-postcode-wildcard)

## Strategy 4

Strategy 4 takes version 14 of Strategy 3, splits the given name into an array, and passes them to PDS as multiple given names.

### Version 1

1. [Non Fuzzy GFD](rules.md#non-fuzzy-gfd)
2. [Fuzzy GFD](rules.md#fuzzy-gfd)
3. [Fuzzy All](rules.md#fuzzy-all)
4. [Non Fuzzy GFD Range](rules.md#non-fuzzy-gfd-range)
5. [Non Fuzzy GFD Range (Postcode)](rules.md#non-fuzzy-gfd-range-postcode)
6. [Fuzzy GFD Range](rules.md#fuzzy-gfd-range)
7. [Fuzzy GFD Range (Postcode)](rules.md#fuzzy-gfd-range-postcode)

### Version 2

1. [Non Fuzzy GFD](rules.md#non-fuzzy-gfd)
2. [Fuzzy GFD](rules.md#fuzzy-gfd)
3. [Fuzzy All](rules.md#fuzzy-all)
5. [Non Fuzzy GFD Range (Postcode)](rules.md#non-fuzzy-gfd-range-postcode)
7. [Fuzzy GFD Range (Postcode)](rules.md#fuzzy-gfd-range-postcode)

## Strategy 5

Strategy 5 builds on Version 2 of Strategy 4 by adding two new queries that omit the `GivenName` field.

### Why this version exists

Initial analysis suggested that the GivenName field sometimes contains "noisy" data, such as:

- Multiple names or middle names merged into one field.
- Special characters or formatting errors.
- Temporary placeholders (e.g., "baby" or "infant") used before a formal name is registered.

The theory behind this strategy is that by omitting the GivenName in specific scenarios, 
the Personal Demographics Service (PDS) can better identify an individual using its internal name history and other demographic anchors, rather than being blocked by a non-matching first-name string.

### What we did to get here

We started with Strategy 4 Version 2, as it was the most effective version at the time. 
We then inserted two new queries (steps 3 and 4 below) after our most effective initial rules.
This ensures that we first attempt to match using the most accurate data (including first name) to minimize risk, before falling back to the GivenName-omitted queries for the remaining unmatched records.
Note that the queries in this strategy were run on all records and were not limited to only those that had quality issues.

### What we found as a result of the changes

When running this strategy against our dataset,
we found that while these new queries had only a small effect on the specific records they were designed to target, 
they had an unexpectedly larger impact on overall results, improving the match rate by several percentage points and elevating some previously non-confident matches to confident ones.

### Version 1

1. [Non Fuzzy GFD](rules.md#non-fuzzy-gfd)
2. [Fuzzy GFD](rules.md#fuzzy-gfd)
3. [Fuzzy FDG postcode](rules.md#fuzzy-fdrange-gender-postcode)
4. [Fuzzy FD postcode](rules.md#fuzzy-fdrange-postcode)
5. [Fuzzy All](rules.md#fuzzy-all)
6. [Non Fuzzy GFD Range (Postcode)](rules.md#non-fuzzy-gfd-range-postcode)
7. [Fuzzy GFD Range (Postcode)](rules.md#fuzzy-gfd-range-postcode)

