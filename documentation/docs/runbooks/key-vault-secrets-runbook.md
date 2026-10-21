# Key Vault secrets run book

## Purpose

This run book explains how to add a one-off secret by hand to a stack's Key Vault in the Azure portal. The secrets each
stack needs are listed in that stack's run book:

- [Blob processor local run book](blob-processor-local-runbook.md#add-the-nhs-digital-secrets-to-key-vault)
- [GraphQL processor local run book](graphql-processor-local-runbook.md#add-the-secrets-to-key-vault)

## Key Vault network access by stack

| Stack | Run book | Key Vault public network access |
| --- | --- | --- |
| `blob-event-processor` | [Blob processor local run book](blob-processor-local-runbook.md) | Disabled |
| `api-batch-processor` | [GraphQL processor local run book](graphql-processor-local-runbook.md) | Enabled |

The setting comes from the `publicNetworkAccess` parameter passed to `infra/modules/shared/secrets.bicep` in each
stack's `main.bicep`. Update this table if that changes.

## Prerequisites

- The infrastructure deployment has created the Key Vault. Its name is in the `SECRETS_VAULT_NAME` deployment output.
- The **Key Vault Secrets Officer or Admin** role on the Key Vault. The Key Vault uses Azure RBAC, so network access alone does
  not give you access to secrets.

## Add a secret when public network access is enabled

1. Open the Key Vault named in the `SECRETS_VAULT_NAME` deployment output.
2. Go to **Objects** > **Secrets** > **Generate/Import** and add the secret.

## Add a secret when public network access is disabled

The portal reaches the Key Vault over the public internet, so it is blocked. Temporarily allow your IP address, add the
secret, then disable public access again.

1. Open the Key Vault named in the `SECRETS_VAULT_NAME` deployment output.
2. Go to **Settings** > **Networking**, select **Allow public access from specific virtual networks and IP addresses**,
   select **Add your client IP address**, then select **Apply**.
3. Go to **Objects** > **Secrets** > **Generate/Import** and add the secret.
4. Go back to **Networking**, select **Disable public access**, then select **Apply**.

The next infrastructure deployment also disables public access, however this should always be set back to disabled as soon as possible.
