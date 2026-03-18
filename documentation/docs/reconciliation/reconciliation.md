# Reconciliation

## Links

[Address Parsing](./address_parsing.md)

## Overview
Reconciliation is a core feature of the SUI Matcher system that enables users to validate their local patient records against the national Personal Demographics Service (PDS). By uploading a set of records (typically via CSV), the system performs a multi-stage comparison to ensure data accuracy, identify discrepancies, and provide actionable insights for data cleansing.

## Why Reconciliation?
The primary goal of reconciliation is to ensure that the client has the correct person identified in their local records. By comparing local data against the PDS (the national "gold standard" for patient demographics), the process provides:
- **Source of Truth Verification:** Confirms whether local records align with the national record.
- **Data Gap Identification:** Highlights missing information in local systems that is available nationally.
- **Discrepancy Resolution:** Pinpoints exactly where local data (e.g., names, DOB, addresses) differs from the national record, allowing users to investigate and correct errors.
- **Confidence in Matching:** Provides a indication of how well a local record aligns with a PDS entry, identifying which records are safely linked and which may require manual intervention.

## Key Components & Outputs

The reconciliation process enriches the input data with several calculated fields (prefixed with `SUI_` in the output) to provide a comprehensive view of the record's status.

### 1. Match Status (`SUI_MatchStatus`)
This indicates the outcome of the initial search against PDS. It reflects the system's confidence in linking the provided local demographics to a specific national record, ranging from high-confidence automatic matches to cases where no record could be found or where manual review is required due to ambiguity.

### 2. Field Comparisons
These components help identify exactly how data diverges between the two systems.
- **Differences (`SUI_Differences`):** Lists fields where data exists in both the local record and PDS, but the values are different (e.g., local = 'Stuart' and PDS = 'Stewart').
- **Missing Local (`SUI_MissingLocalFields`):** Identifies fields that did not contain any information in the request.
- **Missing NHS (`SUI_MissingNhsFields`):** Identifies fields that did not contain any information from the PDS response.

### 3. Address Comparisons
To help identify discrepancies in address data, several specific fields are included:
- **Primary Address Same:** A boolean check if the current primary address in both systems is identical.
- **Address Histories Intersect:** A boolean check  if there is any historical overlap between the addresses known to the local system and those recorded in PDS.
- **Primary CMS Address in PDS History:** A boolean check to determines if the address the user currently considers "Primary" was previously known to PDS.
- **Primary PDS Address in CMS History:** A boolean check to determines if the address PDS considers "Primary" exists anywhere in the local system's historical records.

### 4. Processing Metadata
- **Status (`SUI_Status`):** The final reconciliation state for the record. Possible values include:
    - `LocalDemographicsDidNotMatchToAnNhsNumber`: The provided details (name, DOB, etc.) could not be linked to any record.
    - `LocalNhsNumberIsNotValid`: The provided NHS number is technically invalid.
    - `LocalNhsNumberIsNotFoundInNhs`: The NHS number provided does not exist in the national record.
    - `LocalNhsNumberIsSuperseded`: The NHS number is no longer active and has been replaced.
    - `NoDifferences`: The local record and the national record are in full agreement.
    - `Differences`: The record was successfully linked, but specific demographic fields contain different values.
    - `Error`: An unidentified error occurred during processing.
