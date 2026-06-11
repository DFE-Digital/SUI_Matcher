# Blob processor local run book

## Purpose

The purpose of this run book is to provide instructions for deployment to a azure cloud environment for the blob processor
application. This run book is intended for using azure CLI from your local machine.

## Quick reference - A short “at a glance” section for people who already understand the process

TODO:

## Prerequisites

- Azure CLI (az) installed and logged in to the correct subscription and tenant.
- Permission and access to deploy to the target azure environment.

## Deployment steps overview

1. [Pick a branch, tag or commit to deploy](#pick-a-branch-tag-or-commit-to-deploy).
2. [Run dotnet restore, build and run all tests to ensure the code is in a good state](#run-dotnet-restore-build-and-tests).
3. [Set the deployment environment variables](#set-the-deployment-environment-variables).
4. [Run the infrastructure what-if to validate the output](#run-the-infrastructure-what-if-to-validate-the-output).
5. [Run the infrastructure deployment](#run-the-infrastructure-deploy).

    **Note** this will fail on first run for the applications as they will not yet exist in the azure container registry,
but this is expected. The second run will succeed as the applications will have been built and pushed to the registry by the first run.

6. Run the API' and Storage processor deployment which will deploy the API and Storage processor applications to the Azure container registry.
7. Run the infrastructure deployment again to deploy the applications to the environment.

    **Note** this will succeed as the applications will now exist in the azure container registry.
8. Run the smoke tests to validate the deployment. You can do this by placing a CSV file in the blob storage and checking the logs of the storage processor to see if it has processed the file.

## Detailed deployment steps - Follow in order

### Pick a branch, tag or commit to deploy. Ensure the branch is clean of any uncommitted changes. Example for main

```bash
git checkout main
git pull
```  

### Run dotnet restore, build and tests

```bash
dotnet restore
dotnet build --no-restore
dotnet test
```

### Set the deployment environment variables

Run these commands from the repository root.

The local command mirrors the GitHub workflow
`.github/workflows/gh-blob-event-processor-infra-deploy.yml` and the reusable action
`.github/actions/stack-infra-run/action.yml`.

The GitHub workflow's `AZURE_CLIENT_ID` value is only needed for GitHub OIDC login. It is not needed when running
the what-if locally with an interactive `az login`.

This example uses Bash, matching the GitHub workflow. On Windows, run it from WSL or Git Bash to use the `.env` file
and Bash commands as shown.

First, create a `.env-blob-event-processor` file in the root of the repository based on the template below and fill in your target values:

```
# .env file

# Target values.
AZURE_ENV_NAME="Prod"
AZURE_ENV_PREFIX="<environment-prefix>"
AZURE_TENANT_ID="<tenant-id>"
AZURE_SUBSCRIPTION_ID="<subscription-id>"
AZURE_LOCATION="<azure-region>"

# Infrastructure values.
AZURE_MONITORING_ACTION_GROUP_EMAIL="<monitoring-alert-email-address>"
AZURE_CONTAINER_APP_MANAGED_ENVIRONMENT_NUMBER="<managed-environment-number>"
AZURE_CONTAINER_APP_VNET="<container-app-vnet-cidr>"
AZURE_CONTAINER_APP_ENV_SUBNET="<container-app-environment-subnet-cidr>"
AZURE_CONTAINER_APP_PE_SUBNET="<private-endpoint-subnet-cidr>"

# Workflow defaults. Change these only when the target deployment requires it.
AZURE_INCLUDE_ROLE_ASSIGNMENTS="true"
AZURE_TURN_ON_ALERTS="false"
STORAGE_PROCESS_JOB_IMAGE_TAG="latest"
MATCHING_API_IMAGE_TAG="latest"
EXTERNAL_API_IMAGE_TAG="latest"
RESOURCE_GROUP_MODE="create" # "create" or "existing"
TARGET_RESOURCE_GROUP_NAME=""
STORAGE_ACCOUNT_MODE="create" # "create" or "existing"
EXISTING_STORAGE_ACCOUNT_NAME="" # Leave blank if it does not exist

```

Then, load the variables into your terminal session and derive the deployment values. Existing check overrides

> **Windows PowerShell Users**: Native PowerShell does not support `source .env` or the Bash examples as written. Use WSL
> or Git Bash, or translate the setup into PowerShell syntax before running the Azure CLI command.

```bash
source .env

STACK_RESOURCE_GROUP="${AZURE_ENV_PREFIX}-$(printf '%s' "$AZURE_ENV_NAME" | tr '[:upper:]' '[:lower:]')-blob-event-processor"

if [ "${RESOURCE_GROUP_MODE}" = "existing" ]; then
  STACK_RESOURCE_GROUP="${TARGET_RESOURCE_GROUP_NAME}"
fi

DEPLOYMENT_NAME="${STACK_RESOURCE_GROUP}-$(printf '%s' "$AZURE_LOCATION" | tr '[:upper:]' '[:lower:]')-what-if"
TEMPLATE_FILE="infra/stacks/blob-event-processor/subscription.bicep"
```

### Run the infrastructure what-if to validate the output

Check the required values before running the what-if:

```bash
set -euo pipefail

if [ -z "${AZURE_CONTAINER_APP_PE_SUBNET}" ]; then
  echo "AZURE_CONTAINER_APP_PE_SUBNET must be set for blob-event-processor infrastructure deployments."
  exit 1
fi

if [ "${RESOURCE_GROUP_MODE}" = "existing" ] && [ -z "${TARGET_RESOURCE_GROUP_NAME}" ]; then
  echo "TARGET_RESOURCE_GROUP_NAME must be set when RESOURCE_GROUP_MODE is existing."
  exit 1
fi

if [ "${STORAGE_ACCOUNT_MODE}" = "existing" ] && [ -z "${EXISTING_STORAGE_ACCOUNT_NAME}" ]; then
  echo "EXISTING_STORAGE_ACCOUNT_NAME must be set when STORAGE_ACCOUNT_MODE is existing."
  exit 1
fi
```

Sign in, select the subscription, and run the what-if:

```bash
az login --tenant "${AZURE_TENANT_ID}"
az account set --subscription "${AZURE_SUBSCRIPTION_ID}"

echo "Target stack resource group: ${STACK_RESOURCE_GROUP}"
echo "Deployment name: ${DEPLOYMENT_NAME}"

az deployment sub what-if \
  --name "${DEPLOYMENT_NAME}" \
  --location "${AZURE_LOCATION}" \
  --subscription "${AZURE_SUBSCRIPTION_ID}" \
  --template-file "${TEMPLATE_FILE}" \
  --parameters \
    environmentName="${AZURE_ENV_NAME}" \
    environmentPrefix="${AZURE_ENV_PREFIX}" \
    location="${AZURE_LOCATION}" \
    monitoringActionGroupEmail="${AZURE_MONITORING_ACTION_GROUP_EMAIL}" \
    containerAppManagedEnvironmentNumber="${AZURE_CONTAINER_APP_MANAGED_ENVIRONMENT_NUMBER}" \
    containerAppVnet="${AZURE_CONTAINER_APP_VNET}" \
    containerAppEnvSubnet="${AZURE_CONTAINER_APP_ENV_SUBNET}" \
    containerAppPeSubnet="${AZURE_CONTAINER_APP_PE_SUBNET}" \
    includeRoleAssignments="${AZURE_INCLUDE_ROLE_ASSIGNMENTS}" \
    turnOnAlerts="${AZURE_TURN_ON_ALERTS}" \
    storageProcessJobImageTag="${STORAGE_PROCESS_JOB_IMAGE_TAG}" \
    matchingApiImageTag="${MATCHING_API_IMAGE_TAG}" \
    externalApiImageTag="${EXTERNAL_API_IMAGE_TAG}" \
    resourceGroupMode="${RESOURCE_GROUP_MODE}" \
    targetResourceGroupName="${TARGET_RESOURCE_GROUP_NAME}" \
    storageAccountMode="${STORAGE_ACCOUNT_MODE}" \
    existingStorageAccountName="${EXISTING_STORAGE_ACCOUNT_NAME}"
```
