# Strategies

In order to find a record in PDS which matches the demographic data provided, we apply a set of rules. Each ordered combination of rules is referred to as a strategy. When these have minor improvements or bug fixes applied, a new version of this strategy is created and supercedes the previous version. When substantial changes are made, a new strategy is created.

## Key definitions

The following defitions are taken from [NHS FHIR API Search](https://digital.nhs.uk/developer/api-catalogue/personal-demographics-service-fhir#get-/Patient).

### Fuzzy
A fuzzy search:

- matches common homophones, such as Smith and Smythe, using the [Soundex](https://en.wikipedia.org/wiki/Soundex) algorithm
- checks for transposed names, such as Adam Thomas and Thomas Adam
- always searches history, such as previous names and addresses

It is more likely to include multiple matches and false positives than a non-fuzzy search.

### Non-Fuzzy
A non-fuzzy search:

- allows wildcards in names and postcodes
- does not match homophones or transposed names
- can optionally search history, such as previous names and addresses

It is less likely to return multiple matches than a fuzzy search.

### Exact
An exact search will return a result if the demographic details match exactly. This might be useful if you are verifying an NHS number against details you already believe to be correct. It is unlikely to work well with wildcards or fuzzy search.

### GFD
Given name, Family name, Date of Birth.

### Postcode wildcard
Searches the postcode with only the first two letters of the first half specified, widening the search when postcodes may be inaccurate.

## Strategies

### Strategy 1

1. [Exact GFD](rules.md#exact-gfd)
2. [Exact All](rules.md#exact-all)
3. [Fuzzy GFD](rules.md#fuzzy-gfd)
4. [Fuzzy All](rules.md#fuzzy-all)
5. [Fuzzy GFD Range](rules.md#fuzzy-gfd-range)
6. [Fuzzy Alt DOB](rules.md#fuzzy-alt-dob)

### Strategy 2

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

### Strategy 3

Strategy 3 went through many iterations during testing, and the three most recent versions are documented below. For more details, please see the full version history [here](strategy3.md).

#### Version 14

1. [Non-Fuzzy GFD](rules.md#non-fuzzy-gfd)
3. [Fuzzy GFD](rules.md#fuzzy-gfd)
4. [Fuzzy All](rules.md#fuzzy-all) 
5. [Non-Fuzzy GFD Range](rules.md#non-fuzzy-gfd-range)
5. [Non-Fuzzy GFD Range (Postcode)](rules.md#non-fuzzy-gfd-range-postcode)
6. [Fuzzy GFD Range](rules.md#fuzzy-gfd-range)
7. [Fuzzy GFD Range (Postcode)](rules.md#fuzzy-gfd-range-postcode)

#### Version 15

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

#### Version 16

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

### Strategy 4

Strategy 4 takes version 14 of Strategy 3, splits the given name into an array, and passes them to PDS as multiple given names.

1. [Non Fuzzy GFD](rules.md#non-fuzzy-gfd)
2. [Fuzzy GFD](rules.md#fuzzy-gfd)
3. [Fuzzy All](rules.md#fuzzy-all)
4. [Non Fuzzy GFD Range](rules.md#non-fuzzy-gfd-range)
5. [Non Fuzzy GFD Range (Postcode)](rules.md#non-fuzzy-gfd-range-postcode)
6. [Fuzzy GFD Range](rules.md#fuzzy-gfd-range)
7. [Fuzzy GFD Range (Postcode)](rules.md#fuzzy-gfd-range-postcode)

## Return Results

Each query to PDS via the matcher returns one of the following results:

### NHS_NUM
A confident match to a single NHS number has been found, and that number is returned.

### NO_MATCH
There are no entries in PDS which could be matched to the NHS number. This may suggest that the demographic information provided does not represent anyone stored within PDS, or that either PDS or the searcher contain inaccurate demographic information.

### POTENTIAL_MATCH
A match was found in PDS which does not meet the confident match criteria, but is close to that criteria. This may indicate a slight difference in demographic information.

### LOW_CONFIDENCE_MATCH
Only one entry in PDS was found to contain any demographic information in common, but the records are different enough to cast doubt on the certainty of the match. This may indicate out-of-date demographic information, such as a name or address change.

### MANY_MATCHES
Multiple entries in PDS could be matched to the demographic information given. This may indicate duplicate entries in PDS, or that the person is linked to multipl NHS numbers.
