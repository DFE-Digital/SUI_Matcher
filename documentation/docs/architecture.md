# Architecture

These are the architecture documents. 

## Constraints and Principals

This project is aimed at providing a solution for Local Authorities that require more accurate matching capabilities.

It requires a use-case to be agreed with NHS England to allow the use of the PDS FHIR API. This will require a number of governance steps to be followed, such as the [DTAC](https://transform.england.nhs.uk/key-tools-and-info/digital-technology-assessment-criteria-dtac/).

The PDS FHIR API has a limitation of 5TPS for requests, and the "fallback" logic used in the pilot means each match to a person may use >1 request. This means the PDS FHIR API is expected to initially be the biggest bottleneck for performance.

For principles, please refer to the [DfE Technical Guidance](https://technical-guidance.education.gov.uk/principles/general/) and [Secure by Design Principles](https://www.security.gov.uk/policy-and-guidance/secure-by-design/).

## Logical Architecture

Placeholder

## Application Architecture 

Placeholder

## Information Architecture

Placeholder

## Non Functional Requirements 

Placeholder

## Non Functional Priorities 

The Non-Functional Priorities for the pilot are listed below.

1. **Security** - Very sensitive data must be protected.
2. **Usability** - Easy for both internal teams and external partners (e.g., Local Councils) to use and adopt.
3. **Compatibility** - Integration with existing systems like the NHS and Wigan's ecosystem.
4. **Maintainability** - Highly iterative development process anticipated.
5. **Reliability** - Not initially business-critical. Reliable enough for the pilot phase.
6. **Performance** - Handling relatively small workloads during the pilot phase.
7. **Availability** - Emphasis on maintenance and ensuring sufficient uptime.
8. **Portability** - Targeted at limited settings for the pilot.
9. **Scalability** - May scale from 1 to 4 Local Authorities (LAs) quickly, this would require these priorities to change. Future hosting patterns to be reviewed.
