# Blob processor local runbook

## Purpose

The pupose of this runbook is to provide instructions for deployment to a azure cloud environment for the blob processor
application. This runbook is intended for using azure cli from your local machine.

## Quick reference - A short “at a glance” section for people who already understand the process.

TODO:

## Prerequisites

- Azure CLI (az) installed and logged in to the correct subscription and tenant.
- Permission and access to deploy to the target azure environment.

## Deployment steps overview

1. Pick a branch, tag or commit to deploy.
2. Run dotnet restore, build and run all tests to ensure the code is in a good state.
3. Run the infrastructure what-if to validate the output.
4. Run the infrastructure deployment which will first deploy the infrastructure and then deploy the application to the environment.

    **Note** this will fail on first run for the applications as they will not yet exist in the azure container registry,
but this is expected. The second run will succeed as the applications will have been built and pushed to the registry by the first run.

5. Run the API' and Storage processor deployment which will deploy the API and Storage processor applications to the Azure container registry.
6. Run the infrastructure deployment again to deploy the applications to the environment.
7. Run the smoke tests to validate the deployment. You can do this by placing a csv file in the blob storage and checking the logs of the storage processor to see if it has processed the file.

## Detailed deployment steps

### Step 1: Pick a branch, tag or commit to deploy.

### Step 2: Run dotnet restore, build and run all tests to ensure the code is in a good state.

```bash
dotnet restore
dotnet build --no-restore
dotnet test
```

### Step 3: Run the infrastructure deployment which will first deploy the infrastructure and then deploy the application to the environment.

#### Step 3a: Run the what-if to validate the output.


