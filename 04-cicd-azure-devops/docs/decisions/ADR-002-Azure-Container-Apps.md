# ADR-002: Azure Container Apps for Deployment

## Status
Accepted

## Context
We need a container orchestration platform for deploying the API. Options include Azure Kubernetes Service, Azure App Service, and Azure Container Apps.

## Decision
Use Azure Container Apps for deployment.

## Consequences

### Positive
- Serverless container hosting
- Built-in scaling
- Lower cost than AKS for small workloads
- Easy Docker deployment
- Built-in ingress

### Negative
- Less control than AKS
- Limited to specific regions
- Less mature than App Service

## Alternatives Considered
1. **Azure App Service**: Simpler but less container-native
2. **Azure Kubernetes Service**: More powerful but complex and expensive
3. **Azure Container Instances**: Good for batch jobs, not web apps
