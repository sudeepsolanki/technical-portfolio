# AGENTS.md — RAG Application

Resume notes for `05-ragApplication`. Keep this updated so a new session can pick up where the last one left off.

User-facing docs live in [`README.md`](README.md) (functionality, architecture, RAG explained, run/config/API) and in the portfolio-level [`../README.md`](../README.md). Keep them in sync with this file.

## What this is

A retrieval-augmented-generation (RAG) sample app for the technical portfolio:

- **Ingest**: upload file → extract text in a **Web Worker** (pdf.js for PDFs, streaming Excel extractor for `.xlsx`/`.xlsm`, SheetJS only for legacy `.xls`, plain text otherwise) → POST the text → backend **queues an async ingestion job** (chunk → embed → store vectors in Postgres pgvector). The API returns immediately (`202`) and the frontend polls per-document `Status` (`queued | processing | ready | failed`).
- **Ask**: embed the question → cosine similarity search → build a context prompt → chat completion.
- Documents can be listed and the question can be **scoped to one or more selected documents**.

**Stack**

- Backend: .NET 10 (ASP.NET Core), clean architecture, `Microsoft.Extensions.AI` + OpenAI-compatible client.
- DB: PostgreSQL 16 with the `pgvector` extension, **persisted to a bind mount** (see Run).
- Frontend: Angular 19 (standalone components, signals, `HttpClient`), `pdfjs-dist` (PDF text), `xlsx`/SheetJS (`xls`), `fflate` (streaming xlsx). Extraction lives in a **Web Worker** (`src/app/workers/extract.worker.ts`), so heavy PDF/Excel parsing never blocks the UI thread.
- AI provider: Ollama (reached via SSH tunnel on the host at `localhost:11434`): `qwen2.5:3b` chat, `nomic-embed-text` embeddings, **768 dims**.

**Excel extraction**: XLSX/XLS cells are rendered as text — a header row followed by
`Header: value` pairs per row, one sheet section per sheet — so Q&A can address rows
("What is Sudeep's salary?"). Detection is by extension/MIME/OLE magic; `.xls` (BIFF) is
read via SheetJS too.
- Files go through SheetJS (`xlsx`) **only for legacy `.xls` (OLE/BIFF)**. Every `.xlsx`/`.xlsm`
  (any size) uses `frontend/src/app/services/excel-stream.ts` — a custom **streaming** extractor
  (fflate `Inflate` + incremental TextDecoder + hand-rolled XML scanner) that never materialises
  the decompressed workbook, so no file can OOM the tab ("Array buffer allocation failed").
  Output format matches the SheetJS path. SheetJS still OOM'd on a 337 KB workbook whose content
  SheetJS simply inflates pathologically — hence no size threshold anymore.

## Layout

```
backend/
  RagApp.Domain/          Document, DocumentChunk (no infra deps)
  RagApp.Application/     IRagService, IVectorStore, RagService, AIGatewayException, DocumentInfo
  RagApp.Infrastructure/  RagDbContext, EfVectorStore, DependencyInjection
  RagApp.WebApi/          Program.cs, Controllers/{Chat,Documents}Controller
frontend/
  src/app/services/       rag.service.ts (API), documents.store.ts (list + multi-select state),
                          extract-text.ts (pure extraction logic), excel-stream.ts (streaming xlsx)
  src/app/workers/        extract.worker.ts (Web Worker entry; created via `new Worker(new URL(...))`)
  src/app/components/     upload (worker-based extraction + progress polling), chat (scope chips + Q&A)
  src/assets/             pdf.worker.min.mjs (copy of pdfjs-dist worker)
docker-compose.yml        postgres + backend + frontend
RagApp.slnx               .NET solution
.vscode/                  launch.json (API + Edge), tasks.json (build + ng serve)
```

## How to run

**Full stack (Docker)**

```
docker compose up --build
# frontend http://localhost:4200, api http://localhost:8080/swagger
```

Postgres data persists on the host at `/media/dell/Work/database-test`
(bind mount; the dir is owned by uid 999 = the postgres user). Ollama must be
reachable on the host at `:11434` (in this workspace it's an SSH tunnel).

**Local dev (VS Code)**

- Task `RAG: build backend (WebApi)`.
- Task `RAG: ng serve (Angular)`.
- Launch `RAG API (Debug)` on `http://localhost:8080` (Development).
- Needs Postgres (e.g. `docker compose up -d postgres`) and Ollama on `localhost:11434`.

**Key commands**

```
dotnet build backend/RagApp.WebApi/RagApp.WebApi.csproj
npm start --prefix frontend      # ng serve
npm run build --prefix frontend  # ng build
```

No test projects exist yet.

## Important gotchas

- **Embedding dimension**: `RagDbContext` sizes the `vector()` column from
  `Rag:EmbeddingDimension` (appsettings default 768; fallback 1536). It must
  match the embedding model. If the app runs with a *wrong content root*
  (e.g. `dotnet <path>.dll` launched from the repo root instead of
  `backend/RagApp.WebApi/`), `appsettings.json` isn't loaded and the column
  becomes `vector(1536)` → `expected 1536 dimensions, not 768` write errors.
  Always run with cwd = `backend/RagApp.WebApi` (as the VS Code launch task does).
- Because schema comes from `EnsureCreated()`, a dimension change requires
  dropping the tables (`DROP TABLE "DocumentChunks" CASCADE; DROP TABLE "Documents" CASCADE;`).
- `Document.Content` is sanitized (trimmed, NUL bytes stripped) before chunking/insert.
- PDF detection in the browser is by **magic bytes** (`%PDF-`), extension, or MIME — a file named
  without `.pdf` is still parsed. The backend now *rejects* any content starting with `%PDF-`
  or a zip (`PK\x03\x04`) header (raw binary slipped through silently before → garbage embeddings → "I don't know" answers).
- AI calls in `RagService.AiGuardAsync` map failures to `AIGatewayException` → HTTP 502.
- The parent `technical-portfolio/` folder is the git repo; `05-ragApplication/` is currently untracked. The repo `.gitignore` lists `.vscode/`.

## API

- `POST /api/documents/upload` — JSON `{ title, content }` (text extracted client-side). Returns **`202 Accepted`** with `{ id, status: "processing", message }`; ingestion runs in the background.
- `GET  /api/documents` — returns `[{ id, title, createdAt, chunkCount, status, error, totalChunks, chunksCompleted, isReady }]`.
- `GET  /api/documents/{id}` — same shape for one document (used for upload progress polling).
- `DELETE /api/documents/{id}` — cancels any in-flight ingestion job, then deletes document + chunks (404 if unknown).
- `POST /api/chat/ask` — `{ question, documentIds?: string[] }`; omit/empty `documentIds` to search all documents.

## Async ingestion (this session)

- `RagService.StartIngestionAsync` validates (incl. `%PDF-` / `PK\x03\x04` guards), inserts the
  document row (`Status='queued'`), then fires a `Task.Run` background job with its **own DI scope**
  (the request scope dies before embedding finishes). In-flight jobs are tracked in a static
  `ConcurrentDictionary<Guid, JobState>` so a DELETE can cancel the job and swap the request-scoped
  CancellationToken for a job-scoped one.
- Embeddings are **batched + concurrent**: `Rag:EmbeddingBatchSize` (16) chunks per `GenerateAsync`
  call and `Rag:EmbeddingConcurrency` (2) parallel batches → a 500-chunk upload is ~16 calls instead
  of 500. Chunk order is preserved (results indexed by position; M.E.AI guarantees ordering).
- Progress (and failure) is persisted via `EfVectorStore.UpdateDocumentAsync`, which **reuses the
  already-tracked entity** — a blind `Attach()` of a second instance throws "already being tracked"
  on the 2nd consecutive update (caught earlier: the failure/progress row froze).
- `UpdateDocumentAsync` marks **only** `Status`/`Error`/`TotalChunks`/`ChunksCompleted` as `IsModified`
  (per-property), never the whole entity: `Attach()` marks an entity Unchanged so values set on the DTO
  wouldn't otherwise reach SQL, but marking the whole entity Modified made the minimal status DTO clobber
  the stored **`Title`/`Content` with empty strings** on every progress update (the background job uses a
  fresh scope, so the document isn't tracked → `Attach` of the empty DTO → full-row UPDATE). Verified this
  session: a NUL-test upload was stored with `Title=""`/`Content` empty before the per-property fix,
  preserved after it.
- Schema columns `Status` / `Error` / `TotalChunks` / `ChunksCompleted` are added idempotently at
  startup via `ALTER TABLE ... ADD COLUMN IF NOT EXISTS` (EnsureCreated won't alter existing tables);
  pre-existing rows default to `Status='ready'`.
- Ollama retries 4x on a dead tunnel, so a downed gateway shows as `failed` after ~10-30 s (not a hung upload).

## Completed recently

- Postgres persistence via host bind mount `/media/dell/Work/database-test`.
- PDF + **Excel** text extraction (pdf.js + SheetJS) in the browser — fixes "answers are 'I don't know'"
  (previous code stored raw PDF/zip bytes as "content").
- Excel extraction is **sparse** (iterates only real cells, never `sheet_to_json`/`!ref`) so stray
  far-away cells can't blow memory ("Array buffer allocation failed"); uploads > 25 MB are rejected
  with a clear message.
- **Workbook answer**: `xlsx` (SheetJS) crashes even on a 337 KB workbook with pathological
  inflated content ("Array buffer allocation failed") and on 10–25 MB workbooks. The streaming
  extractor (`excel-stream.ts`, fflate + incremental XML) handles 120k-row/38 MB (~13 s) and
  300k-cell/13 MB (~2.4 s) sheets at flat memory; validated in Node against a
  shared-strings/rich-text/CDATA/entities crafted workbook, sheetjs-generated files, and the
  stress cases.
- Document list + multi-select scope in chat; backend scopes search by `DocumentIds`.
- Document **delete** (backend DELETE endpoint + trash-style button in the upload pane, with confirm dialog).
- **Async upload** (this session): upload returns `202` immediately, ingestion runs in the background
  with per-chunk progress (`chunksCompleted/totalChunks`), and the UI polls it. Batched (`batch=16`)
  + concurrent (`=2`) embeddings replaced the sequential per-chunk loop (~500 round-trips → ~16 calls);
  configurable via `Rag:*` settings.
- **Web Worker extraction**: `pdf.js`/`xlsx`/excel-stream now run in `extract.worker.ts` (main bundle is
  clean of them; dev/prod emit a separate `extract-worker` chunk via `new Worker(new URL(...))`). PDFs are
  passed to pdf.js as raw `data` bytes (no blob-URL fetch problems in workers; memory isolated from the
  page heap, bounded by the 25 MB cap).
- Upload pane shows live progress ("embedded X / N chunks…") plus `QUEUED / PROCESSING… / FAILED` chips
  on each document row; deleting a processing doc cancels the backend job and stops polling.
- Removed stray `debugger;` in `rag.service.ts`.
- `onEnter` now submits only on Enter (was firing on every keydown).
- Header tag updated from "9router" → "Ollama".
- **Row-aware chunking** (this session): `RagService.ChunkText` no longer cuts 500-char windows through
  records. Spreadsheets emit one row per line (`Label: value, ...`), so chunks are built by grouping
  whole lines up to `ChunkSize` — a record (row) is **never split**; an oversized line (>2000 chars)
  falls back to fixed windows so embeddings stay bounded; overlap is ignored (it only duplicated rows).
  Fixes wrong Q&A on wide sheets: `PurchaseRegistorData_2025_01.xlsx` went 3,349 fragmented chunks →
  1,722 whole-row chunks (0 rows split, verified in Node against the real extraction).
- `AskAsync` retrieval bumped `matchCount` 4 → 8 (whole rows ≈ 515 chars, so 8 chunks ≈ 8 records).
- **Docs** (this session): added the user-facing `README.md` here (what it is, the two RAG phases, ingest/ask flows, browser-side extraction table, run/config/API) and rewrote the portfolio-level `../README.md` to add project 05 and fix the stale directory paths.
- **Ingestion data-loss fix** (this session): verified the NUL sanitize in a rebuilt image (NUL upload now `202` → `ready`, NUL stripped) and found it exposed a worse bug — `UpdateDocumentAsync` was marking the *whole* DTO entity `Modified`, so every progress update wrote empty `Title`/`Content` over the stored row (background job's fresh scope → entity not tracked → `Attach` of the minimal status DTO). Now marks only the four progress columns `IsModified`. Verified in Docker: `Docker Guide` uploaded, `Title`/`Content`/chunks/`totalChunks` all preserved and the scoped ask returned the right answer.

## Open items / leftovers

- [ ] `Program.cs` fallback defaults still point at a 9router URL (`localhost:20128`) + hardcoded API key; use Ollama defaults or fail fast.
- [ ] Upload is client-extracted JSON text; a `multipart/file` endpoint + server-side extraction would be more robust than worker-only pdf.js.
- [ ] No formal migrations — schema is `EnsureCreated` + idempotent `ALTER TABLE ... ADD COLUMN IF NOT EXISTS` for this session's status columns; a real migration tool would be safer.
- [ ] Runtime dependency: Ollama here is reached via an SSH remote-forward tunnel on `localhost:11434` — if that tunnel drops, uploads fail (job ends `failed` after retries) and asks fail with "Connection refused"; re-establish the tunnel. For Docker, the forward must be `-L 0.0.0.0:11434:localhost:11434` (a plain `-L 11434:localhost:11434` binds `127.0.0.1` only, so `host.docker.internal:11434` is refused from containers); the host-debug path works with either.
- [ ] In-flight ingestion survives neither delete-during-embed (by design, delete cancels it) nor app restart (job state is in-memory); acceptable for a demo.
- [ ] Tune `Rag:EmbeddingConcurrency` / `EmbeddingBatchSize` against the Ollama tunnel's actual throughput.
- [ ] Re-ingest `PurchaseRegistorData_2025_01.xlsx` with the new row-aware chunker: delete the old doc
  (3,349 fragmented chunks) and re-upload (should become ~1,722 whole-row chunks), then re-run the user's
  questions. Blocked on the Ollama tunnel being up. Note: `totalChunks` reads 0 on both stored docs even
  though `chunksCompleted` > 0 — reconcile during re-ingest (suspect they were ingested before the
  totalChunks fix landed). Note: a live re-ingest started with the pre-fix code (`b0706fce…`, 1722
  chunks) had its `Title`/`Content` clobbered to empty by the old `UpdateDocumentAsync`; its chunks
  still hold the text so search works, but re-ingest it (or restore the row) to get the title back.
- [ ] Aggregate/numeric questions ("total purchase value in December", "top brand by volume") are weak even
  with whole-row chunks — pure RAG can't sum across rows reliably. A structured/SQL answer path is the
  proper fix if those are the frequent question type.
- [ ] The resume `Sudeep-Resume-10-Oct-26` was accidentally deleted by an earlier cleanup loop and
  re-ingested (21 chunks, `ready`) from `/home/dell/Desktop/Sudeep-Document/Sudeep-Resume-10-Oct-26`
  via `pdftotext -layout`. Be careful with any batch-delete logic.