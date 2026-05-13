# Strategy 3 Full Version History

#### Version 1

Simplest test to establish a baseline using all fields in both fuzzy and non-fuzzy searches.

1. [Non-Fuzzy All](rules.md#non-fuzzy-all)
2. [Fuzzy All](rules.md#fuzzy-all)

#### Version 2

Adds an additional postcode wildcard rule to V1.

1. [Non-Fuzzy All (Postcode Wildcard)](rules.md#non-fuzzy-all-postcode-wildcard)
2. [Non-Fuzzy All](rules.md#non-fuzzy-all)
3. [Fuzzy All](rules.md#fuzzy-all)

#### Version 3

Adds an additional fuzzy postcode wildcard rule to V2.

1. [Non-Fuzzy All (Postcode Wildcard)](rules.md#non-fuzzy-all-postcode-wildcard)
2. [Non-Fuzzy All](rules.md#non-fuzzy-all)
3. [Fuzzy GFD (Postcode Wildcard)](rules.md#fuzzy-gfd-postcode-wildcard)
4. [Fuzzy All](rules.md#fuzzy-all)

#### Version 4

Adds a non-fuzzy GFD with DOB range rule to V1.

1. [Non-Fuzzy GFD Range](rules.md#non-fuzzy-gfd-range)
2. [Non-Fuzzy All](rules.md#non-fuzzy-all)
3. [Fuzzy All](rules.md#fuzzy-all)

#### Version 5

Adds a fuzzy GFD with DOB range rule to V1.

1. [Fuzzy GFD Range](rules.md#fuzzy-gfd-range)
2. [Non-Fuzzy All](rules.md#non-fuzzy-all)
3. [Fuzzy All](rules.md#fuzzy-all)

#### Version 6

Adds a postcode field to the first rule in V4.

1. [Non-Fuzzy GFD Range (Postcode)](rules.md#non-fuzzy-gfd-range-postcode)
2. [Non-Fuzzy All](rules.md#non-fuzzy-all)
3. [Fuzzy All](rules.md#fuzzy-all)


#### Version 7

Adds an additional postcode wildcard rule to V6.

1. [Non-Fuzzy GFD Range (Postcode)](rules.md#non-fuzzy-gfd-range-postcode)
2. [Non-Fuzzy GFD Range (Postcode Wildcard)](rules.md#non-fuzzy-gfd-range-postcode-wildcard)
3. [Non-Fuzzy All](rules.md#non-fuzzy-all)
4. [Fuzzy All](rules.md#fuzzy-all)

#### Version 8

Changes the first two rules in V7 to be fuzzy.

1. [Fuzzy GFD Range (Postcode)](rules.md#fuzzy-gfd-range-postcode)
2. [Fuzzy GFD Range (Postcode Wildcard)](rules.md#fuzzy-gfd-range-postcode-wildcard)
3. [Non-Fuzzy All](rules.md#non-fuzzy-all)
4. [Fuzzy All](rules.md#fuzzy-all)

#### Version 9

Combines the first two steps of V7 and V8 to have non-fuzzy and fuzzy searches.

1. [Non-Fuzzy GFD Range (Postcode)](rules.md#non-fuzzy-gfd-range-postcode)
2. [Non-Fuzzy GFD Range (Postcode Wildcard)](rules.md#non-fuzzy-gfd-range-postcode-wildcard)
3. [Fuzzy GFD Range (Postcode)](rules.md#fuzzy-gfd-range-postcode)
4. [Fuzzy GFD Range (Postcode Wildcard)](rules.md#fuzzy-gfd-range-postcode-wildcard)
5. [Non-Fuzzy All](rules.md#non-fuzzy-all)
6. [Fuzzy All](rules.md#fuzzy-all)

#### Version 10

Adds fuzzy and non-fuzzy GFD searches to V9 and separates fuzzy and non-fuzzy searching, ordered for performance within each category.

1. [Non-Fuzzy GFD](rules.md#non-fuzzy-gfd)
2. [Non-Fuzzy GFD Range (Postcode)](rules.md#non-fuzzy-gfd-range-postcode)
3. [Non-Fuzzy GFD Range (Postcode Wildcard)](rules.md#non-fuzzy-gfd-range-postcode-wildcard)
4. [Non-Fuzzy All](rules.md#non-fuzzy-all)
5. [Fuzzy GFD](rules.md#fuzzy-gfd)
6. [Fuzzy GFD Range (Postcode)](rules.md#fuzzy-gfd-range-postcode)
7. [Fuzzy GFD Range (Postcode Wildcard)](rules.md#fuzzy-gfd-range-postcode-wildcard)
8. [Fuzzy All](rules.md#fuzzy-all)

#### Version 11

Re-orders the rules from V10 for performance, removing postcode wildcards, and adding non-fuzzy GFD range.

1. [Non-Fuzzy GFD](rules.md#non-fuzzy-gfd)
2. [Non-Fuzzy GFD Range](rules.md#non-fuzzy-gfd-range)
3. [Fuzzy GFD](rules.md#fuzzy-gfd)
4. [Fuzzy All](rules.md#fuzzy-all)
5. [Non-Fuzzy GFD Range (Postcode)](rules.md#non-fuzzy-gfd-range-postcode)
6. [Fuzzy GFD Range (Postcode)](rules.md#fuzzy-gfd-range-postcode)
7. [Non-Fuzzy All](rules.md#non-fuzzy-all)

#### Version 12

Re-orders the rules from V11 by most matches found.

1. [Non-Fuzzy GFD](rules.md#non-fuzzy-gfd)
3. [Fuzzy GFD](rules.md#fuzzy-gfd)
4. [Fuzzy All](rules.md#fuzzy-all)
5. [Non-Fuzzy GFD Range](rules.md#non-fuzzy-gfd-range)
5. [Non-Fuzzy GFD Range (Postcode)](rules.md#non-fuzzy-gfd-range-postcode)
6. [Fuzzy GFD Range (Postcode)](rules.md#fuzzy-gfd-range-postcode)
7. [Non-Fuzzy All](rules.md#non-fuzzy-all)

#### Version 13

Adds "Fuzzy GFD Range" to V12. Removes "Non-Fuzzy All" as it found no additional records in V11 and V12.

1. [Non-Fuzzy GFD](rules.md#non-fuzzy-gfd)
3. [Fuzzy GFD](rules.md#fuzzy-gfd)
4. [Fuzzy All](rules.md#fuzzy-all)
5. [Non-Fuzzy GFD Range](rules.md#non-fuzzy-gfd-range)
6. [Fuzzy GFD Range](rules.md#fuzzy-gfd-range)
5. [Non-Fuzzy GFD Range (Postcode)](rules.md#non-fuzzy-gfd-range-postcode)
6. [Fuzzy GFD Range (Postcode)](rules.md#fuzzy-gfd-range-postcode)

#### Version 14

Moves "Fuzzy GFD Range" down as it found fewer result than "Non-Fuzzy GFD Range (Postcode)".

1. [Non-Fuzzy GFD](rules.md#non-fuzzy-gfd)
3. [Fuzzy GFD](rules.md#fuzzy-gfd)
4. [Fuzzy All](rules.md#fuzzy-all)
5. [Non-Fuzzy GFD Range](rules.md#non-fuzzy-gfd-range)
5. [Non-Fuzzy GFD Range (Postcode)](rules.md#non-fuzzy-gfd-range-postcode)
6. [Fuzzy GFD Range](rules.md#fuzzy-gfd-range)
7. [Fuzzy GFD Range (Postcode)](rules.md#fuzzy-gfd-range-postcode)

#### Version 15

Adds postcode wildcard rules to the end of V14.

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
