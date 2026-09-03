# .NET Technical Portfolio

A collection of production-style .NET 9 projects demonstrating modern software engineering practices, architecture patterns, CI/CD pipelines, and distributed systems.

## Portfolio Overview

| # | Project | Focus Area | Tech Stack |
|---|---------|-----------|------------|
| 01 | [Clean Architecture Product API](01-clean-architecture/) | Architecture & Design Patterns | .NET 9, EF Core, React, FluentValidation |
| 02 | [Microservices with Saga Pattern](02-microservices/) | Distributed Systems | .NET 9, MassTransit, RabbitMQ, YARP |
| 03 | [GitHub Actions CI/CD](03-cicd-github-actions/) | DevOps & Automation | .NET 9, GitHub Actions, Docker |
| 04 | [Azure DevOps CI/CD](04-cicd-azure-devops/) | Enterprise DevOps | .NET 9, Azure DevOps, Docker, ACA |

## Projects

### 01 - Clean Architecture Product Catalog

A Product Catalog API demonstrating Clean Architecture (Onion Architecture) with strict dependency inversion. The Domain layer has zero infrastructure dependencies.

**Key Concepts:** Clean Architecture, SOLID Principles, Repository Pattern, CQRS-like Separation, FluentValidation

[View Project →](01-clean-architecture/)

---

### 02 - Microservices with Saga Pattern

A distributed order processing system with 4 microservices communicating via RabbitMQ. Implements the Choreography-based Saga pattern for distributed transactions with automatic compensation.

**Key Concepts:** Saga Pattern, Event-Driven Architecture, Database Per Service, API Gateway, Idempotency, MassTransit

[View Project →](02-microservices/)

---

### 03 - GitHub Actions CI/CD

An Order Management API with a complete CI/CD pipeline using GitHub Actions. Includes PR validation, build, test, Docker build, and multi-environment deployment with approval gates.

**Key Concepts:** GitHub Actions, CI/CD Pipelines, Docker, Environment Promotion, Approval Gates

[View Project →](03-cicd-github-actions/)

---

### 04 - Azure DevOps CI/CD

A Customer Management API demonstrating enterprise-grade CI/CD with Azure DevOps Pipelines. Multi-stage YAML pipeline with variable groups, approval gates, and Azure Container Apps deployment.

**Key Concepts:** Azure DevOps Pipelines, Multi-Stage YAML, Variable Groups, Azure Container Apps, Enterprise Deployment

[View Project →](04-cicd-azure-devops/)

## Architecture Diagrams

Architecture diagrams for all projects are in the [architecture/](architecture/) folder.

## Tech Stack Summary

- **Runtime:** .NET 9
- **Web:** ASP.NET Core Minimal APIs, Swagger/OpenAPI
- **ORM:** Entity Framework Core 9
- **Testing:** xUnit, FluentAssertions, Moq
- **Frontend:** React 19, TypeScript, Vite
- **Messaging:** RabbitMQ, MassTransit
- **Gateway:** YARP Reverse Proxy
- **Containerization:** Docker, Docker Compose
- **CI/CD:** GitHub Actions, Azure DevOps Pipelines
- **Cloud:** Azure Container Apps

## Running Locally

Each project can be run independently:

```bash
# 01 - Clean Architecture
cd 01-clean-architecture-product-api
dotnet run --project src/ProductCatalog.API

# 02 - Microservices
cd 04-dotnet-microservices-grpc-saga
docker compose up -d

# 03 - GitHub Actions CI/CD
cd 02-github-actions-cicd
dotnet run --project src/OrderApi

# 04 - Azure DevOps CI/CD
cd 03-azure-devops-cicd
dotnet run --project src/CustomerApi
```

## Author

Built as a demonstration of .NET development capabilities across architecture, distributed systems, and DevOps practices.
