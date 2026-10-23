# Microsoft Entra ID authentication (optional)

Set `AZURE_AUTH_MODE=entra` to authenticate outbound Azure OpenAI and Azure AI Search calls using `DefaultAzureCredential`. For local development, use `az login`; in Azure, prefer a managed identity with least-privilege data-plane RBAC. The app does not store bearer tokens.

- Azure OpenAI scope: `https://cognitiveservices.azure.com/.default`. Assign the identity an appropriate Azure OpenAI data-plane role (for example, Cognitive Services OpenAI User).
- Azure AI Search scope: `https://search.azure.com/.default`. Assign the identity Search Index Data Reader for queries and Search Index Data Contributor for document writes/deletes. Configure Search service role-based data-plane access.

The default `AZURE_AUTH_MODE=key` retains compatibility with existing `AZURE_OPENAI_API_KEY` and `AZURE_SEARCH_API_KEY` settings. In Entra mode, these keys are optional.

This change authenticates **outbound cloud requests only**. It does not authenticate users of the CLI, enforce tenant/document authorization, provision roles, or replace the need for access-control design.
