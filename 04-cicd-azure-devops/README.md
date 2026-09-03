# Azure DevOps CI/CD - Customer API

A production-style .NET 9 Customer Management API demonstrating enterprise CI/CD implementation with Azure DevOps.

## Overview

This project demonstrates enterprise-grade CI/CD using Azure DevOps Pipelines. It includes:

- Multi-stage YAML pipeline
- Variable groups and secure variables
- Environment promotion (Dev → Staging → Production)
- Approval gates for production deployments
- Docker containerization
- Azure Container Apps deployment

## Architecture

```mermaid
graph TB
    Developer[Developer]
    
    subgraph AzureDevOps["Azure DevOps"]
        Repos[ Repos]
        Pipelines[Pipelines]
        Artifacts[Artifacts]
        Environments[Environments]
    end
    
    subgraph Stages["Pipeline Stages"]
        Build[Build & Test]
        Docker[Docker Build]
        Dev[Deploy Dev]
        Staging[Deploy Staging]
        Prod[Deploy Production]
        Verify[Verify]
    end
    
    subgraph Azure["Azure"]
        ACR[Container Registry]
        ACA_Dev[Container Apps - Dev]
        ACA_Staging[Container Apps - Staging]
        ACA_Prod[Container Apps - Production]
    end
    
    Developer --> Repos
    Repos --> Pipelines
    Pipelines --> Build
    Build --> Docker
    Docker --> Dev
    Dev --> Staging
    Staging --> Prod
    Prod --> Verify
    
    Docker --> ACR
    Dev --> ACA_Dev
    Staging --> ACA_Staging
    Prod --> ACA_Prod
    
    style AzureDevOps fill:#0078d4
    style Stages fill:#e3f2fd
    style Azure fill:#e8f5e9
```

## Project Structure

```
03-azure-devops-cicd/
├── azure-pipelines.yml          # Multi-stage pipeline
├── src/
│   └── CustomerApi/
│       ├── Controllers/         # API endpoints
│       ├── Data/                # EF Core DbContext
│       ├── DTOs/                # Data Transfer Objects
│       ├── Middleware/           # Exception handling, Correlation ID
│       └── Models/              # Entities
├── tests/
│   └── CustomerApi.Tests/       # Unit tests
├── docs/
│   └── decisions/               # ADRs
├── Dockerfile                   # Multi-stage build
├── docker-compose.yml           # Local development
└── README.md
```

## Azure DevOps Pipeline

### Pipeline Stages

```mermaid
graph LR
    Build[Build & Test] --> Docker[Docker Build]
    Docker --> Dev[Deploy Dev]
    Dev --> Staging[Deploy Staging]
    Staging --> Prod[Deploy Prod]
    Prod --> Verify[Verify]
    
    style Build fill:#e3f2fd
    style Docker fill:#f3e5f5
    style Dev fill:#e8f5e9
    style Staging fill:#fff3e0
    style Prod fill:#fce4ec
    style Verify fill:#e0f7fa
```

### Stage Details

#### 1. Build and Test
- Install .NET SDK
- Restore NuGet packages
- Build solution
- Run unit tests with code coverage
- Publish test results
- Create build artifact

#### 2. Docker Build
- Build Docker image
- Push to Azure Container Registry
- Tag with build ID and latest

#### 3. Deploy Development
- Auto-deploy after Docker build
- Deploy to Azure Container Apps
- Run database migrations

#### 4. Deploy Staging
- Deploy to staging environment
- Validation testing

#### 5. Deploy Production
- Requires approval gate
- Deploy to production environment
- Blue-green deployment support

#### 6. Post-deployment Verification
- Health check
- Smoke tests

### Variables

```yaml
variables:
  buildConfiguration: 'Release'
  dotnetVersion: '9.0.x'
  azureSubscription: '$(AZURE_SUBSCRIPTION)'
  containerRegistry: '$(CONTAINER_REGISTRY)'
  imageRepository: 'customer-api'
  tag: '$(Build.BuildId)'
```

### Environments

| Environment | Approval | Auto-deploy |
|-------------|----------|-------------|
| Development | None | Yes |
| Staging | None | Yes |
| Production | Required | No |

### Mermaid Pipeline Diagram

```mermaid
sequenceDiagram
    participant Dev as Developer
    participant ADO as Azure DevOps
    participant Build as Build Stage
    participant Docker as Docker Stage
    participant DevEnv as Development
    participant StagingEnv as Staging
    participant ProdEnv as Production

    Dev->>ADO: Push code
    ADO->>Build: Trigger pipeline
    Build->>Build: Build & Test
    Build->>Docker: Build Docker image
    Docker->>Docker: Push to ACR
    Docker->>DevEnv: Deploy to Dev (auto)
    DevEnv->>StagingEnv: Deploy to Staging (auto)
    StagingEnv->>ProdEnv: Wait for approval
    Dev->>ADO: Approve production
    ADO->>ProdEnv: Deploy to Production
    ProdEnv->>ADO: Health check
```

## Azure Resources

### Required Azure Resources

| Resource | Purpose | SKU |
|----------|---------|-----|
| Azure Container Registry | Store Docker images | Basic |
| Azure Container Apps | Host API | Consumption |
| Azure SQL Database | Data storage | Basic |
| Application Insights | Monitoring | Basic |

### Resource Group Structure

```
rg-customer-api-dev
├── acr-customer-api
├── ca-customer-api
├── sql-customer-api
└── ai-customer-api

rg-customer-api-staging
├── acr-customer-api
├── ca-customer-api
├── sql-customer-api
└── ai-customer-api

rg-customer-api-prod
├── acr-customer-api
├── ca-customer-api
├── sql-customer-api
└── ai-customer-api
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/customers | List all customers |
| GET | /api/customers/{id} | Get customer by ID |
| POST | /api/customers | Create customer |
| PUT | /api/customers/{id} | Update customer |
| DELETE | /api/customers/{id} | Soft delete customer |
| GET | /health | Health check |

## Getting Started

### Prerequisites

- .NET 9 SDK
- Docker (optional)
- Azure account (for deployment)
- Azure DevOps organization

### Local Development

```bash
# Clone repository
git clone <repo-url>
cd 03-azure-devops-cicd

# Run with Docker
docker compose up

# Or run directly
dotnet run --project src/CustomerApi
```

### Azure Setup

1. Create Azure DevOps organization
2. Create project and import repository
3. Create service connections
4. Create variable groups
5. Create environments (dev, staging, production)
6. Configure approval gates for production

### Azure Resources Setup

```bash
# Login to Azure
az login

# Create resource groups
az group create --name rg-customer-api-dev --location eastus
az group create --name rg-customer-api-staging --location eastus
az group create --name rg-customer-api-prod --location eastus

# Create container registry
az acr create --resource-group rg-customer-api-dev --name custapiacr --sku Basic

# Create container apps environment
az containerapp env create --name cae-dev --resource-group rg-customer-api-dev --location eastus
```

## Interview Discussion Points

1. **Why multi-stage pipeline?**
   - Separates build from deployment
   - Different approval requirements per stage
   - Clear audit trail

2. **Why approval gates?**
   - Prevents accidental production deployments
   - Ensures human oversight
   - Compliance requirement

3. **Why variable groups?**
   - Centralized configuration
   - Environment-specific values
   - Secure variable storage

4. **How to handle secrets?**
   - Azure DevOps variable groups
   - Azure Key Vault integration
   - Never commit to repository

5. **Rollback strategy?**
   - Keep previous Docker image tagged
   - Deploy previous version
   - Database migration rollback

## Possible Improvements

- Add SonarQube integration
- Add security scanning (OWASP)
- Add performance testing stage
- Implement canary deployments
- Add monitoring and alerting
- Implement blue-green deployments
- Add database migration automation

## License

MIT License - see [LICENSE](LICENSE)
