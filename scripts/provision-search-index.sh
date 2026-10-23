#!/usr/bin/env bash
set -euo pipefail
: "${AZURE_SEARCH_ENDPOINT:?Set AZURE_SEARCH_ENDPOINT}"
: "${AZURE_SEARCH_INDEX:?Set AZURE_SEARCH_INDEX}"
: "${AZURE_TENANT_ID:?Set AZURE_TENANT_ID}"
: "${AZURE_SEARCH_API_KEY:?Set AZURE_SEARCH_API_KEY (admin key, for provisioning only)}"
command -v python3 >/dev/null || { echo "python3 is required" >&2; exit 1; }
tenant_hash="$(printf '%s' "$AZURE_TENANT_ID" | tr '[:upper:]' '[:lower:]' | sha256sum | cut -c1-16)"
index_name="${AZURE_SEARCH_INDEX}-${tenant_hash}"
payload="$(python3 - "$index_name" "${EMBEDDING_DIMENSIONS:-1536}" <<'PY'
import json, sys
with open('infra/search-index.schema.json', encoding='utf-8') as f:
    schema = json.load(f)
schema['name'] = sys.argv[1]
for field in schema['fields']:
    if field['name'] == 'embedding':
        field['dimensions'] = int(sys.argv[2])
print(json.dumps(schema))
PY
)"
curl --fail-with-body --silent --show-error -X PUT \
  "${AZURE_SEARCH_ENDPOINT%/}/indexes/${index_name}?api-version=2024-07-01" \
  -H "api-key: ${AZURE_SEARCH_API_KEY}" -H 'Content-Type: application/json' \
  --data "$payload"
echo
echo "Provisioned tenant index: $index_name"
