The SUI Matcher contains a matching algorithm, tuned to find an available NHS
number for a given child demographic record. To understand this algorithm, the
main concepts to grasp are [rules](./rules.md) and [strategies](./strategies.md)
-- these are explained below in full with links to related references. Alongside
this, some [key definitions](#key-definitions) are provided to help interpret
the rule and strategy information.

## Rules and strategies, explained

The main tool underlying the SUI Matcher's matching operations, is the [NHS FHIR
API 'Search for a
patient'](https://digital.nhs.uk/developer/api-catalogue/personal-demographics-service-fhir#get-/Patient)
endpoint.

The SUI Matcher calls this API endpoint with a particular combination of inputs;
this combination is refered to as a [Search Rule](./rules.md).  As part of these
calls, the [Search for a
patient](https://digital.nhs.uk/developer/api-catalogue/personal-demographics-service-fhir#get-/Patient)
endpoint can be queried in one of three modes:

1. [Exact search](#exact-search)
2. [Non-fuzzy search](#non-fuzzy-search)
3. [Fuzzy search](#fuzzy-search)

Each rule can return either a match with a confidence score, no match or an
indication that many records match and a single match can't be distinguished.

When processing a demographic record to find a matching NHS number, the SUI
Matcher applies multiple search rules and the best possible match is selected
from the results. The set of rules used to match a record to an NHS number is
referred to as a [Search Strategy](./strategies.md), which may have
multiple versions. A particular search strategy can be configured upon
deployment of the SUI matcher.

Multiple strategies have been created through the lifecycle of the SUI Matcher,
to seek the best possible matching algorithm for the child social care use case.
A full [List of strategies](./strategies.md) and their related rules is
maintained for reference.

The overall output of a search strategy will return one of the following
statuses, alongisde any matched NHS number and demographics of the matched person:

1. *Match.* A confident match has been found, where the NHS number retrieved can
   be used operationally without review. At least one of the rules must have
   returned with a confidence score above the 'Match' threshold, which is 0.95
   by default.

2. *PotentialMatch.* No confident match could be found, but with improvements in
   demographics a confident match may be possible. The highest confidence score
   returned by any of the rules must have been between the potential threshold,
   between 0.85 and 0.95 by default.

3. *LowConfidenceMatch.* No confident match could be found but match data was
   returned. The highest confidence score returned by any of the rules must have
   been lower than the low confidence threshold, 0.85 by default.

4. *ManyMatch.* Multiple people could plausibly match the inputted demographics,
   but it's too close to call. All rules either returned a many match status or
   a no match status.

5. *NoMatch.* Nobody could be found in NHS datasets, even remotely similar to
   the input. All rules returned no match statuses.

6. *Error.* An unexpected error occurred during processing.

## Key definitions

In the rules and strategies, some common terms are used to describe searching
input. These are defined here.

Many of these are taken from [NHS FHIR API
documentation](https://digital.nhs.uk/developer/api-catalogue/personal-demographics-service-fhir#get-/Patient).

### Exact search
An exact search will return a result if the demographic details match exactly.
This might be useful if you are verifying an NHS number against details you
already believe to be correct.

### Non-Fuzzy search

A non-fuzzy search:

- allows wildcards in names and postcodes
- does not match homophones or transposed names
- can optionally search history, such as previous names and addresses

It is less likely to return multiple matches than a fuzzy search.

In other matching terminology, this is known as a deterministic method.

### Fuzzy search

A fuzzy search:

- matches common homophones, such as Smith and Smythe, using the [Soundex](https://en.wikipedia.org/wiki/Soundex) algorithm
- checks for transposed names, such as Adam Thomas and Thomas Adam
- always searches history, such as previous names and addresses

It is more likely to include multiple matches and false positives than a non-fuzzy search.

In other matching terminology, this is known as a probabilistic method.

### GFD

An initialism for: Given name, Family name, Date of Birth.

### Postcode wildcard search

Where postcode fields are only compared using the [postcode area](https://en.wikipedia.org/wiki/Postcodes_in_the_United_Kingdom#Postcode_area), widening the search when postcodes may be inaccurate.

## See also
- [List of rules](./rules.md)
- [List of strategies](./strategies.md)
