# Ways of Working
Quick intro into ways of working.

## Branching
We follow basic branching practics. It follows the standard of prefix/ticket-number/headline-description.

Prefix types can be:

* feature/: For developing new features.
* bugfix/: To fix bugs in the code.
* docs/: Used to write, modify or correct documentation.

## Pull Requests
Pull requets to be approved by one other member of the team. All tests should pass before merging. Merging to be done by the person who raises the pull request so that their name appears on the merge commit.

## Commit Messages
Commit messages should follow on from the branching standards.

    <type>([ticket number]): <description>
    [body]


## CI/CD (Aspirations)
When infra up and running we can work out how to contiunally deploy to the integration environment within the DfE. 

## Testing (Aspirations)
* unit testing of features with no external dependencies.
* integration/e2e test to be performed on pull request. Data set to be developed to test the likely scenarios.

## Documentation
Documentation to be maintained in this repo and published to github.