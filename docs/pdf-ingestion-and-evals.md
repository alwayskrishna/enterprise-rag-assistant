# PDF ingestion and deterministic retrieval evaluation

## Supported ingestion formats

`ask` and `index` accept UTF-8 `.txt` and text-based `.pdf` documents. PDF text is extracted with PdfPig and then passed to the same chunking/retrieval pipeline. Scanned/image-only PDFs require OCR and are intentionally rejected; no OCR or layout/table reconstruction is provided. The current chunker does not preserve page numbers.

```bash
dotnet run --project src/EnterpriseRag -- ask "What does this policy say?" docs/policy.pdf
dotnet run --project src/EnterpriseRag -- index docs/policy.pdf --index-file .data/vectors.json
```

Indexing and semantic retrieval require Azure OpenAI embedding configuration; local `ask` works offline.

## Evaluate lexical retrieval

```bash
dotnet run --project src/EnterpriseRag -- eval evals/cases.json samples/operations.txt
```

Each JSON case specifies `id`, `query`, and `expectedDocumentId`. The runner reports accuracy@1 and exits nonzero on failure. This tests retrieval ranking, **not** factual accuracy, generative answer quality, or production-grade citation validity. Add more documents and representative questions for a meaningful benchmark.
