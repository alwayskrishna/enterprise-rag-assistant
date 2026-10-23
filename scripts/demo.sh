#!/usr/bin/env bash
set -euo pipefail
IMAGE="${1:-enterprise-rag-assistant:local}"
echo "Building container..."
docker build -t "$IMAGE" .
echo "Running offline retrieval demo..."
docker run --rm "$IMAGE" ask "How should latency incidents be handled?"
echo "Running retrieval evaluation..."
docker run --rm "$IMAGE" eval evals/cases.json samples/operations.txt
