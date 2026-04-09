# Address Parsing & Comparison Rules

This document outlines the algorithmic logic used to compare addresses between CMS and PDS records. The logic is
designed to handle the inconsistencies of manual data entry while
operating without external validation tools.

## Core Comparison Logic (AreAddressesEqual)

The comparison follows a strict hierarchy of rules. The process stops as soon as a rule is met.

1. Postcode Validation (Mandatory)
   The very first check is a postcode match.

  - Rule: Postcodes must match exactly (ignoring spaces and casing).
  - Result: If postcodes differ, the addresses are immediately marked as Unmatched.

2. Exact Match (The "Golden" Rule)
   We check if the primary address lines are identical.

  - Rule: Compare Address Line 1 from both records.
  - Result: If they are identical strings, the record is Matched.

3. Building Number Extraction & Fallback
   If strings aren't identical, we extract the "Building Number" (e.g., "12", "12A", or "12-14").

  - Preference: The parser looks at Line 1 first.
  - Fallback: If Line 1 contains no digits, it attempts to extract a number from Line 2.
  - Result: If no building number can be found in either line for both records, they are Unmatched.

4. Flat & Apartment Detection
   This identifies scenarios where one system splits the flat and building number across two lines, while the other
   combines them or omits one.

  - Rule: If one record has numbers in both lines (e.g., Line 1: "Flat 2", Line 2: "16 High St") and the other only has
  one number (e.g., "16 High St"), we check for an intersection.
  - Rule: We also check if the building number from Record A appears anywhere as a word inside Line 2 of Record B.
  - Result: If a cross-line match is found, the status is Uncertain (Reason: FlatMissing).

5. Range Matching
   Handles differences in how multi-building addresses are recorded.

  - Exact Range: "12-14" vs "12-14" = Matched.
  - Partial Range: "12" vs "12-14". The logic checks if the single number falls within the numeric bounds of the range.
  - Result: A number within a range is marked as Uncertain (Reason: NumberRange).

  ---

## Result Definitions

| Status | Meaning                                                                                                |
|---|---------------------------------------------------------------------------------------------------------|
| Matched | High confidence. Postcodes match and primary building identifiers are identical.                        |
| Uncertain | Likely a match, but data is structured differently (e.g., flats or ranges). These should be manually sampled or verified. |
| Unmatched | No identifiable link found between the two address strings, or critical data (like building numbers) is missing. |

## Why this order?

1. Postcodes are the most reliable filter to eliminate noise quickly.
2. Exact matches handle the majority of clean data with zero processing overhead.
3. Number extraction and Flat logic are computationally more expensive and are only used as a "rescue" attempt for
   records that don't match cleanly but might still be the same location.
