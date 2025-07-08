# Audit deploy README

This guide is in addition to the `deploy_readme.md` file and provides specific instructions for deploying the audit feature along with the rest of the application.

## Context

The Audit feature is designed to log user actions within the application, providing an audit trail that can be queried and analyzed later.
This is currently a proof of concept and is not enabled by default in the application, but can be enabled by setting the appropriate environment variable before deployment.

## Prerequisites

* You will need to add the following to the Managed Identity of the Container App so that it can write to the Azure Storage Account:
```text
Scope: Storage
Subscription: <Your subscription name>
Resource: <Your resource group name>
Role: Table Data Contributor
```

## Deploying the Audit Feature

To deploy the audit feature, you need to set the `export AZURE_ENABLE_AUDIT_LOGGING="true"` environment variable, before running the deployment commands.
This will enable the audit logging feature in the application.

If you are using what-if to preview the deployment, you will need to include the `enableAuditLogging` parameter and set to `'true'` in your command. This will allow you to see the changes that will be made to enable audit logging in your Azure resources.

## To run locally:

Set the app hosts launch settings `FeatureToggles__EnableAuditLogging` to `true` and then run the app. 
App host will orchestrate an azurite docker image for storage. 
Then send a match request the normal way (see getting started readme). 
You can use Microsoft Azure Storage Explorer to view the logs in the `AuditLogs` table.