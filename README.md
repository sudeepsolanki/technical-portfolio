# .NET Technical Portfolio

A collection of production-style .NET projects demonstrating modern software engineering practices: architecture patterns, distributed systems, CI/CD pipelines, and applied AI. Each project is self-contained with its own README and can be run independently.

## Portfolio Overview

| # | Project | Focus Area | Tech Stack |
|---|---------|-----------|------------|
| 01 | [Clean Architecture Product Catalog](01-clean-architecture/) | Architecture & Design Patterns | .NET 9, EF Core, FluentValidation, SQL Server, React + TypeScript |
| 02 | [Microservices with Saga Pattern](02-microservices/) | Distributed Systems | .NET 9, MassTransit, RabbitMQ, YARP, SQLite (per service), React |
| 03 | [GitHub Actions CI/CD](03-cicd-github-actions/) | DevOps & Automation | .NET 9, GitHub Actions, Docker |
| 04 | [Azure DevOps CI/CD](04-cicd-azure-devops/) | Enterprise DevOps | .NET 9, Azure DevOps, Docker, Azure Container Apps |
| 05 | [RAG Workbench](05-ragApplication/) | Applied AI / Retrieval | .NET 10, Microsoft.Extensions.AI, pgvector, Ollama, Angular 19 |

## Projects

### 01 — Clean Architecture Product Catalog

A Product Catalog API demonstrating Clean Architecture (Onion Architecture) with strict dependency inversion. The Domain layer has zero infrastructure dependencies; interfaces defined in Domain are implemented by Infrastructure.

**Key concepts:** Clean Architecture, SOLID, Repository Pattern, Result Pattern, FluentValidation, unit + integration tests.

[View project →](01-clean-architecture/)

---

### 02 — Microservices with Saga Pattern

A distributed order-processing system with 4 services (Product, Order, Payment, Gateway) communicating through RabbitMQ, using a choreography-based Saga for distributed transactions with automatic compensation and database-per-service isolation.

**Key concepts:** Saga Pattern, event-driven architecture, database per service, API gateway (YARP), idempotency, MassTransit.

[View project →](02-microservices/)

---

### 03 — GitHub Actions CI/CD

An Order Management API with a complete GitHub Actions setup: PR validation, continuous integration, and CD with environment promotion, Docker builds, and approval gates.

**Key concepts:** GitHub Actions, CI/CD pipelines, Docker, environment promotion, approval gates, secrets management.

[View project →](03-cicd-github-actions/)

---

### 04 — Azure DevOps CI/CD

A Customer Management API demonstrating enterprise-grade CI/CD with Azure DevOps Pipelines: a multi-stage YAML pipeline with variable groups, approval gates, and Azure Container Apps deployment.

**Key concepts:** Azure DevOps Pipelines, multi-stage YAML, variable groups, Azure Container Apps, enterprise deployment.

[View project →](04-cicd-azure-devops/)

---

### 05 — RAG Workbench

A full-stack Retrieval-Augmented Generation application: upload PDFs and spreadsheets, then ask questions answered strictly from your documents. The backend chunks and embeds document text into Postgres (pgvector); the frontend extracts text in a Web Worker and polls asynchronous ingestion progress.

**Key concepts:** RAG, embeddings, vector similarity search (pgvector + HNSW), Clean Architecture, asynchronous background jobs, `Microsoft.Extensions.AI`, Web Workers, Angular signals.

[View project →](05-ragApplication/)

## Architecture Diagrams

Mermaid architecture diagrams for the projects are collected in the [architecture/](architecture/) folder.

## Tech Stack Summary

- **Runtime:** .NET 9 (projects 01–04), .NET 10 (project 05)
- **Web:** ASP.NET Core (controllers + minimal APIs), Swagger/OpenAPI
- **ORM:** Entity Framework Core
- **Databases:** SQL Server, SQLite, PostgreSQL + pgvector
- **Testing:** xUnit, FluentAssertions, Moq
- **Frontend:** React 19 + TypeScript + Vite, Angular 19
- **Messaging:** RabbitMQ, MassTransit
- **Gateway:** YARP reverse proxy
- **AI:** Ollama, `Microsoft.Extensions.AI`, pgvector
- **Containerization:** Docker, Docker Compose
- **CI/CD:** GitHub Actions, Azure DevOps Pipelines
- **Cloud:** Azure Container Apps

## Running Projects Locally

Each project is independent; see its README for details.

```bash
# 01 — Clean Architecture
cd 01-clean-architecture
docker compose up            # or: dotnet run --project src/ProductCatalog.API

# 02 — Microservices
cd 02-microservices
docker compose up -d

# 03 — GitHub Actions CI/CD
cd 03-cicd-github-actions
docker compose up            # or: dotnet run --project src/OrderApi

# 04 — Azure DevOps CI/CD
cd 04-cicd-azure-devops
docker compose up            # or: dotnet run --project src/CustomerApi

# 05 — RAG Workbench
cd 05-ragApplication
docker compose up --build    # frontend :4200, api :8080/swagger
```

## Author

Built as a demonstration of .NET development capabilities across architecture, distributed systems, DevOps, and applied AI.
