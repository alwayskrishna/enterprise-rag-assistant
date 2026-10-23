# GitHub Actions Azure deployment

The manually triggered `.github/workflows/deploy-azure.yml` workflow builds and tests the .NET API, builds a Docker image, then uses GitHub OIDC to log in to Azure, push to ACR, and update an **existing** Container App. There are no Azure credentials or registry passwords in source control.

## Prerequisites

1. Provision the Azure resource group, ACR, and Container App using `infra/main.bicep`. The image must exist in ACR before the initial Container App deployment; see [Azure deployment](azure-deployment.md).
2. Create a Microsoft Entra application or user-assigned managed identity for GitHub Actions. Add a federated identity credential with issuer `https://token.actions.githubusercontent.com`, audience `api://AzureADTokenExchange`, and subject `repo:alwayskrishna/enterprise-rag-assistant:environment:azure-production`. Replace the repository name if forked.
3. Assign that identity `AcrPush` on the registry and the least-privileged Azure role sufficient to update the existing Container App. Avoid subscription-wide Contributor.
4. Create the GitHub environment **azure-production** and enable required reviewer approvals. Configure these **environment variables** (not secrets): `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_RESOURCE_GROUP`, `AZURE_ACR_NAME`, `AZURE_CONTAINER_APP_NAME`. These are identifiers, not passwords.
5. Restrict the deployment environment to the main branch and configure branch protection. The workflow uses the checked-out commit and tags the image by SHA by default.

## Deploy

In GitHub Actions, select **Deploy Azure Container App** → **Run workflow** on the approved branch. Approve the `azure-production` environment when prompted. The workflow does not create Azure resources.

## Rollback

List available revisions:

```bash
az containerapp revision list --name <app> --resource-group <rg> -o table
```

With single-revision mode, redeploy a previously known-good immutable image tag using the same workflow input. Keep an auditable record of the previous commit/tag.

## Security and operational caveats

This is a **manual, approval-gated** deployment; no automatic production release on push. OIDC avoids long-lived Azure credentials. CI tests and container build run before the deployment job. The workflow does not yet perform authenticated Azure smoke tests or automated rollback. Do not enable external ingress without a security review and API gateway protections. Azure costs are incurred only when Azure services are provisioned or used.
