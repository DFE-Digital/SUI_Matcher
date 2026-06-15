# Blob processor local run book

## Purpose

The purpose of this run book is to provide instructions for deployment to a azure cloud environment for the blob processor
application. This run book is intended for using azure CLI from your local machine.

## Prerequisites

- Azure CLI (az) installed and logged in to the correct subscription and tenant.
- Permission and access to deploy to the target azure environment.

## Deployment steps overview

1. [Pick a branch, tag or commit to deploy](#pick-a-branch-tag-or-commit-to-deploy).
2. [Run dotnet restore, build and run all tests to ensure the code is in a good state](#run-dotnet-restore-build-and-tests).
3. [Set the deployment environment variables](#set-the-deployment-environment-variables).
4. [Run the infrastructure what-if to validate the output](#run-the-infrastructure-what-if-to-validate-the-output).
5. [Run the infrastructure deployment](#run-the-infrastructure-deploy).
6. [Publish the API images to Azure Container Registry](#publish-the-api-images-to-azure-container-registry).
7. [Publish the Storage processor image to Azure Container Registry](#publish-the-storage-processor-image-to-azure-container-registry).
8. TODO: Run the infrastructure deployment again to deploy the applications to the environment.
9. Run the smoke tests to validate the deployment. You can do this by placing a CSV file in the blob storage and checking the logs of the storage processor to see if it has processed the file.

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
source .env-blob-event-processor

STACK_RESOURCE_GROUP="${AZURE_ENV_PREFIX}-$(printf '%s' "$AZURE_ENV_NAME" | tr '[:upper:]' '[:lower:]')-blob-event-processor"

if [ "${RESOURCE_GROUP_MODE}" = "existing" ]; then
  STACK_RESOURCE_GROUP="${TARGET_RESOURCE_GROUP_NAME}"
fi

DEPLOYMENT_NAME="${STACK_RESOURCE_GROUP}-$(printf '%s' "$AZURE_LOCATION" | tr '[:upper:]' '[:lower:]')-what-if"
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

```

#### Use for existing resource group

```bash

TEMPLATE_FILE="infra/stacks/blob-event-processor/main.bicep"

echo "Target stack resource group: ${STACK_RESOURCE_GROUP}"
echo "Deployment name: ${DEPLOYMENT_NAME}"

az deployment group what-if \
  --name "${DEPLOYMENT_NAME}" \
  --resource-group "${TARGET_RESOURCE_GROUP_NAME}" \
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
    storageAccountMode="${STORAGE_ACCOUNT_MODE}" \
    existingStorageAccountName="${EXISTING_STORAGE_ACCOUNT_NAME}"



```

### Run the infrastructure deploy

Review the what-if output before running the deployment. This command follows the existing resource group path and deploys
`infra/stacks/blob-event-processor/main.bicep` directly into the target resource group.

> **Note**: this deployment can fail on the first run if the container app images do not exist in the Azure Container Registry yet.
> Publish the API and storage processor images, then run this infrastructure deployment again.

```bash
TEMPLATE_FILE="infra/stacks/blob-event-processor/main.bicep"
DEPLOYMENT_NAME="${STACK_RESOURCE_GROUP}-$(printf '%s' "$AZURE_LOCATION" | tr '[:upper:]' '[:lower:]')-deploy"

echo "Target stack resource group: ${STACK_RESOURCE_GROUP}"
echo "Deployment name: ${DEPLOYMENT_NAME}"

az deployment group create \
  --name "${DEPLOYMENT_NAME}" \
  --resource-group "${TARGET_RESOURCE_GROUP_NAME}" \
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
    storageAccountMode="${STORAGE_ACCOUNT_MODE}" \
    existingStorageAccountName="${EXISTING_STORAGE_ACCOUNT_NAME}"
```

### Publish the API images to Azure Container Registry

Run these commands from the repository root after the infrastructure deployment has created the Azure Container Registry
and Container Apps environment.

This mirrors `.github/workflows/gh-blob-event-processor-api-images.yml`. The images are built in Azure Container
Registry using `az acr build`, and each API image is tagged with the current Git commit hash and `latest`.

```bash
set -euo pipefail

LOWERCASE_ENVIRONMENT_NAME="$(printf '%s' "$AZURE_ENV_NAME" | tr '[:upper:]' '[:lower:]')"
ACR_NAME="${AZURE_ENV_PREFIX}${LOWERCASE_ENVIRONMENT_NAME}bepacr01"
CONTAINER_APPS_ENVIRONMENT_NAME="${AZURE_ENV_PREFIX}-${LOWERCASE_ENVIRONMENT_NAME}-bep-cae-${AZURE_CONTAINER_APP_MANAGED_ENVIRONMENT_NUMBER}"
IMAGE_TAG="$(git rev-parse --short=12 HEAD)"
EXTERNAL_API_IMAGE_REPOSITORY="external-api"
MATCHING_API_IMAGE_REPOSITORY="matching-api"

az config set extension.use_dynamic_install=yes_without_prompt

# Remove any hyphens from the ACR name
ACR_NAME="$(printf '%s' "$ACR_NAME" | tr -d '-')"

ACR_LOGIN_SERVER="$(az acr show \
  --name "${ACR_NAME}" \
  --resource-group "${STACK_RESOURCE_GROUP}" \
  --query loginServer \
  --output tsv)"

CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN="$(az containerapp env show \
  --name "${CONTAINER_APPS_ENVIRONMENT_NAME}" \
  --resource-group "${STACK_RESOURCE_GROUP}" \
  --query properties.defaultDomain \
  --output tsv)"

EXTERNAL_API_HTTP_ENDPOINT="http://external-api.internal.${CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}"
EXTERNAL_API_HTTPS_ENDPOINT="https://external-api.internal.${CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}"

echo "ACR: ${ACR_LOGIN_SERVER}"
echo "External API image tag: ${IMAGE_TAG}"
echo "Matching API image tag: ${IMAGE_TAG}"
echo "External API endpoint for matching API: ${EXTERNAL_API_HTTPS_ENDPOINT}"
```

Build the external API image in Azure Container Registry:

```bash
az acr build \
  --registry "${ACR_NAME}" \
  --image "${EXTERNAL_API_IMAGE_REPOSITORY}:${IMAGE_TAG}" \
  --image "${EXTERNAL_API_IMAGE_REPOSITORY}:latest" \
  --file src/external-api/Dockerfile \
  --build-arg ASPNETCORE_ENVIRONMENT="${AZURE_ENV_NAME}" \
  .
```

Build the matching API image in Azure Container Registry:

```bash
az acr build \
  --registry "${ACR_NAME}" \
  --image "${MATCHING_API_IMAGE_REPOSITORY}:${IMAGE_TAG}" \
  --image "${MATCHING_API_IMAGE_REPOSITORY}:latest" \
  --file src/matching-api/Dockerfile \
  --build-arg ASPNETCORE_ENVIRONMENT="${AZURE_ENV_NAME}" \
  --build-arg EXTERNAL_API_HTTP_ENDPOINT="${EXTERNAL_API_HTTP_ENDPOINT}" \
  --build-arg EXTERNAL_API_HTTPS_ENDPOINT="${EXTERNAL_API_HTTPS_ENDPOINT}" \
  .
```

Check that both repositories contain the current Git commit hash and `latest` tags:

```bash
az acr repository show-tags \
  --name "${ACR_NAME}" \
  --repository "${EXTERNAL_API_IMAGE_REPOSITORY}" \
  --output table

az acr repository show-tags \
  --name "${ACR_NAME}" \
  --repository "${MATCHING_API_IMAGE_REPOSITORY}" \
  --output table
```

### Publish the Storage processor image to Azure Container Registry

Run these commands from the repository root after the API images have been published.

This mirrors `.github/workflows/gh-blob-event-processor-storage-job-image.yml`. The image is built in Azure Container
Registry using `az acr build`, and the storage processor image is tagged with the current Git commit hash and `latest`.

```bash
set -euo pipefail

LOWERCASE_ENVIRONMENT_NAME="$(printf '%s' "$AZURE_ENV_NAME" | tr '[:upper:]' '[:lower:]')"
ACR_NAME="${AZURE_ENV_PREFIX}${LOWERCASE_ENVIRONMENT_NAME}bepacr01"
CONTAINER_APPS_ENVIRONMENT_NAME="${AZURE_ENV_PREFIX}-${LOWERCASE_ENVIRONMENT_NAME}-bep-cae-${AZURE_CONTAINER_APP_MANAGED_ENVIRONMENT_NUMBER}"
IMAGE_TAG="$(git rev-parse --short=12 HEAD)"
IMAGE_REPOSITORY="sui-client-storage-process-job"

az config set extension.use_dynamic_install=yes_without_prompt

# Remove any hyphens from the ACR name
ACR_NAME="$(printf '%s' "$ACR_NAME" | tr -d '-')"

ACR_LOGIN_SERVER="$(az acr show \
  --name "${ACR_NAME}" \
  --resource-group "${STACK_RESOURCE_GROUP}" \
  --query loginServer \
  --output tsv)"

CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN="$(az containerapp env show \
  --name "${CONTAINER_APPS_ENVIRONMENT_NAME}" \
  --resource-group "${STACK_RESOURCE_GROUP}" \
  --query properties.defaultDomain \
  --output tsv)"

MATCH_API_BASE_ADDRESS="https://matching-api.internal.${CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}"

echo "ACR: ${ACR_LOGIN_SERVER}"
echo "Storage processor image tag: ${IMAGE_TAG}"
echo "Matching API base address: ${MATCH_API_BASE_ADDRESS}"
```

Build the storage processor image in Azure Container Registry:

```bash
az acr build \
  --registry "${ACR_NAME}" \
  --image "${IMAGE_REPOSITORY}:${IMAGE_TAG}" \
  --image "${IMAGE_REPOSITORY}:latest" \
  --file src/SUI.Client/SUI.Client.StorageProcessJob/Dockerfile \
  --build-arg MATCH_API_BASE_ADDRESS="${MATCH_API_BASE_ADDRESS}" \
  --build-arg CSV_DATE_FORMAT=yyyy-MM-dd \
  --build-arg CSV_COLUMN_ID=Id \
  --build-arg CSV_COLUMN_GIVEN=GivenName \
  --build-arg CSV_COLUMN_FAMILY=FamilyName \
  --build-arg CSV_COLUMN_BIRTH_DATE=DOB \
  --build-arg CSV_COLUMN_EMAIL=EMAIL \
  --build-arg CSV_COLUMN_POSTCODE=POSTCODE \
  --build-arg CSV_COLUMN_GENDER=GENDER \
  --build-arg CSV_COLUMN_PHONE=PHONE \
  --build-arg CSV_COLUMN_NHS_NUMBER=NHS_NUMBER \
  .
```

Check that the repository contains the current Git commit hash and `latest` tags:

```bash
az acr repository show-tags \
  --name "${ACR_NAME}" \
  --repository "${IMAGE_REPOSITORY}" \
  --output table
```
