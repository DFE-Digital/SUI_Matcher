# Deploying `main.bicep` Using Azure CLI

This guide explains how to deploy the `main.bicep` file locally using the Azure Developer CLI (`azd`). 
It also covers using the `main.parameters.json` file for environment-specific configurations.

## Prerequisites

1. **Azure Developer CLI Installed**: Ensure the Azure Developer CLI (`azd`) is installed and authenticated. You can install it [here](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd).
2. **Resource Group**: Verify that the target Azure resource group exists.

## Configuring the Deployment

1. Update `main.parameters.json`  
   The file contains placeholder parameters values that need to be replaced with actual values.

## Running the Deployment

1. Open a terminal and navigate to the directory containing the `main.bicep` file.
2. Log in using a service principal:
   ```bash
   azd auth login --client-id <CLIENT_ID> --tenant-id <TENANT_ID> --client-secret <CLIENT_SECRET>
    ```
3. Run the following command to provision the infrastructure using `azd`:

    ```bash
    azd provision --no-prompt --environment <your-environment-name>
    ```
   Replace `<your-environment-name>` with the name of your environment (e.g., `Integration`).

4. Monitor the deployment progress in the terminal. If successful, you will see a message indicating that the deployment was completed.