# RAG Workbench

A full-stack **Retrieval-Augmented Generation (RAG)** application: upload documents, then ask questions and get answers grounded in *your* files instead of the model's general knowledge.

- **Backend:** .NET 10 Web API (Clean Architecture), `Microsoft.Extensions.AI` with an OpenAI-compatible client.
- **Database:** PostgreSQL 16 + [pgvector](https://github.com/pgvector/pgvector) for vector similarity search.
- **Frontend:** Angular 19 (standalone components + signals).
- **AI provider:** [Ollama](https://ollama.com) — `qwen2.5:3b` for chat, `nomic-embed-text` for embeddings (768 dimensions).

---

## What is RAG?

A large language model only knows what it was trained on. Ask it about *your* PDF or spreadsheet and it will either guess or say it doesn't know. **Retrieval-Augmented Generation** fixes that by giving the model relevant excerpts of your own data at question time.

The idea in one sentence:

> **Retrieve** the most relevant pieces of your documents, then **generate** an answer using only those pieces as context.

A simple analogy: instead of asking someone to answer from memory, you hand them the three most relevant pages of a book and say *"answer using only these pages."*

### The two phases

**1. Ingestion (indexing)** — done once per document:

```
Document text → split into chunks → turn each chunk into a vector (embedding) → store in Postgres
```

**2. Retrieval + generation (asking)** — done for every question:

```
Question → embedding → find nearest chunk vectors → build a context prompt → chat model → answer
```

An **embedding** is a list of numbers (a vector) that captures the *meaning* of text. Texts with similar meaning end up close together in this vector space, so "find the nearest vectors to the question" is effectively "find the most semantically relevant text." Here each vector has **768 numbers**.

---

## How this app works

```
┌──────────────────────────── Angular frontend ────────────────────────────┐
│  Upload pane                                  Chat pane                   │
│    • drag & drop a file                        • ask a question           │
│    • extract text in a Web Worker              • optionally scope to        │
│    • POST the text to the API                    selected documents        │
│    • poll ingestion progress                   • render the answer         │
└───────────────────────────────┬──────────────────────────────────────────┘
                                │ HTTP (JSON)
┌───────────────────────────────▼──────────────────────────────────────────┐
│                          .NET 10 Web API                                  │
│   POST /api/documents/upload  →  202 Accepted (job queued)                │
│   POST /api/chat/ask          →  grounded answer                          │
│                                                                           │
│   Background ingestion job:  chunk → embed (batched) → store vectors      │
└───────────────────────────────┬──────────────────────────────────────────┘
                                │
                ┌───────────────▼───────────────┐
                │  PostgreSQL 16 + pgvector      │
                │  Documents, DocumentChunks     │
                │  (HNSW cosine index)           │
                └───────────────────────────────┘
                                │
                ┌───────────────▼───────────────┐
                │  Ollama (chat + embeddings)    │
                └───────────────────────────────┘
```

### Ingest flow (async)

Ingestion is **asynchronous** so large files never block the request or the UI:

1. The browser reads the file and **extracts plain text in a Web Worker** (see below), then `POST`s `{ title, content }`.
2. The API validates the text, saves a `Document` row with `Status = "queued"`, starts a background job, and immediately returns **`202 Accepted`** with the document id.
3. The background job chunks the text, embeds the chunks (batched + concurrent), stores the vectors, and updates progress (`chunksCompleted / totalChunks`) on the row.
4. The frontend **polls** `GET /api/documents/{id}` (~1 s) and shows live progress until `Status` is `ready` (or `failed`, with the error).

Document status lifecycle:

```
queued → processing → ready
                    ↘ failed
```

### Ask flow

1. The question is embedded with the same embedding model.
2. Postgres returns the **8 nearest chunks** by cosine distance (optionally restricted to the documents the user selected).
3. Those chunks are assembled into a prompt:

   > *"Answer the user's question using ONLY the provided context snippets. If you don't know the answer, say 'I don't know based on the provided documents.'"*

4. The chat model produces the final answer, which is returned to the UI.

### Browser-side text extraction

The backend only ever stores **text**, so extraction happens on the client, off the UI thread, in a Web Worker (`src/app/workers/extract.worker.ts`):

| File type | Extractor |
|-----------|-----------|
| `.pdf` | `pdfjs-dist` (detected by extension, MIME, or `%PDF-` magic bytes) |
| `.xlsx` / `.xlsm` | custom **streaming** extractor (`excel-stream.ts`) — `fflate` + incremental XML parsing, flat memory |
| `.xls` (legacy OLE/BIFF) | SheetJS (`xlsx`) |
| everything else | plain text |

Spreadsheets are rendered as readable text — a header row followed by `Header: value` pairs per row, one section per sheet — so questions like *"What is Sudeep's salary?"* work against rows.

The backend **rejects** raw binary that slips through (content starting with `%PDF-` or a `PK\x03\x04` zip header) so binary bytes are never embedded as garbage.

---

## Project structure

```
05-ragApplication/
├── backend/
│   ├── RagApp.Domain/          # Document, DocumentChunk — no infrastructure dependencies
│   ├── RagApp.Application/     # IRagService, IVectorStore, RagService, RagOptions, DTOs
│   ├── RagApp.Infrastructure/  # RagDbContext (EF Core), EfVectorStore, DI wiring
│   ├── RagApp.WebApi/          # Program.cs, ChatController, DocumentsController
│   └── Dockerfile
├── frontend/
│   └── src/app/
│       ├── components/upload/  # drag & drop, worker extraction, progress polling, delete
│       ├── components/chat/    # Q&A, document scope chips
│       ├── services/           # rag.service.ts (API), documents.store.ts (state),
│       │                       # extract-text.ts, excel-stream.ts
│       ├── workers/            # extract.worker.ts
│       └── assets/             # pdf.worker.min.mjs
├── docker-compose.yml          # postgres + backend + frontend
└── RagApp.slnx
```

The **Clean Architecture** dependency direction is `WebApi → Application → Domain` and `Infrastructure → Application → Domain`. The Domain layer has zero infrastructure dependencies; `DocumentChunk.Embedding` is a plain `float[]` and only the Infrastructure layer knows about `Pgvector.Vector`.

---

## Running it

### Prerequisites

- Docker + Docker Compose (easiest), **or** .NET 10 SDK + Node 20 + a local Postgres for development.
- An AI provider reachable over HTTP. This sample uses **Ollama** exposing an OpenAI-compatible API:
  ```bash
  ollama pull qwen2.5:3b
  ollama pull nomic-embed-text
  ollama serve          # listens on http://localhost:11434
  ```

### Full stack with Docker

```bash
docker compose up --build
```

| Service | URL |
|---------|-----|
| Frontend | http://localhost:4200 |
| API + Swagger | http://localhost:8080/swagger |
| PostgreSQL | localhost:5432 |

The compose file expects the AI provider on the host at `http://host.docker.internal:11434/v1`. If Ollama runs on another machine, forward its port to the host first (e.g. an SSH tunnel `-L 0.0.0.0:11434:localhost:11434`).

Postgres data is persisted to a host bind mount (`/media/dell/Work/database-test` in this workspace) so uploads survive container restarts.

### Local development (VS Code)

- Task **RAG: build backend (WebApi)**
- Task **RAG: ng serve (Angular)**
- Launch **RAG API (Debug)** on `http://localhost:8080` (Development)

You still need Postgres (`docker compose up -d postgres`) and Ollama on `localhost:11434`.

> **Important:** run the API with its working directory set to `backend/RagApp.WebApi/`. Running the built DLL from elsewhere means `appsettings.json` is not loaded, configuration falls back to hardcoded defaults, and the vector column is created with the wrong dimension.

### Key commands

```bash
dotnet build backend/RagApp.WebApi/RagApp.WebApi.csproj
npm start --prefix frontend      # ng serve
npm run build --prefix frontend  # ng build
```

---

## Configuration

Backend settings (`appsettings.json`, overridable by environment variables):

| Key | Default | Purpose |
|-----|---------|---------|
| `AI_ROUTER_URL` | `http://localhost:11434/v1` | OpenAI-compatible base URL (Ollama) |
| `AI_API_KEY` | `ollama` | API key (Ollama ignores it but the client requires one) |
| `AI_CHAT_MODEL` | `qwen2.5:3b` | Chat/completion model |
| `AI_EMBEDDING_MODEL` | `nomic-embed-text` | Embedding model |
| `Rag:EmbeddingDimension` | `768` | **Must** match the embedding model and the existing vector column |
| `Rag:ChunkSize` | `500` | Target characters per chunk |
| `Rag:ChunkOverlap` | `50` | Overlap (currently ignored by row-aware chunking) |
| `Rag:EmbeddingBatchSize` | `16` | Chunks per embedding API call |
| `Rag:EmbeddingConcurrency` | `2` | Embedding batches in flight |
| `Rag:ProgressUpdateEvery` | `64` | Persist progress every N chunks |

Because the schema is created with EF Core `EnsureCreated()`, changing `EmbeddingDimension` requires dropping the tables (or the volume):

```sql
DROP TABLE "DocumentChunks" CASCADE;
DROP TABLE "Documents" CASCADE;
```

---

## API reference

| Method | Endpoint | Body / Notes |
|--------|----------|--------------|
| `POST` | `/api/documents/upload` | `{ "title": "...", "content": "..." }` → **202** `{ id, status, message }` (ingestion runs in the background) |
| `GET` | `/api/documents` | List documents with `status`, `chunkCount`, `totalChunks`, `chunksCompleted`, `isReady` |
| `GET` | `/api/documents/{id}` | One document (used for progress polling) |
| `DELETE` | `/api/documents/{id}` | Cancels any in-flight job, then deletes the document and its chunks |
| `POST` | `/api/chat/ask` | `{ "question": "...", "documentIds": ["..."] }` — omit `documentIds` to search everything |

AI-gateway failures (e.g. Ollama unreachable) are mapped to **HTTP 502** with a clean JSON error.

---

## How the pieces connect (design notes)

- **Async ingestion** — `RagService.StartIngestionAsync` persists the document, then fires a `Task.Run` background job in its **own DI scope** (the request scope is disposed long before embedding finishes). In-flight jobs are tracked in a static registry so `DELETE` can cancel them.
- **Batched + concurrent embeddings** — a 500-chunk document makes ~16 embedding calls instead of 500. Chunk order is preserved by writing results back by index.
- **pgvector search** — the `Embedding` column is `vector(768)`, mapped from `float[]` via a value converter so the Domain stays Pgvector-free. An **HNSW** index with `vector_cosine_ops` accelerates `OrderBy(c => c.Embedding.CosineDistance(query))`.
- **Row-aware chunking** — chunks are built by grouping whole lines up to `ChunkSize`, so a spreadsheet record is never split across chunks (only a pathologically long single line falls back to fixed windows).

See [`AGENTS.md`](AGENTS.md) for the running engineering notes and known limitations.
