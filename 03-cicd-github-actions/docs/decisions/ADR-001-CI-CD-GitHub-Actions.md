# ADR-001: CI/CD with GitHub Actions

## Status
Accepted

## Context
We need automated build, test, and deployment pipelines for the Order API. The solution must support pull request validation, continuous integration, and continuous deployment with environment promotion.

## Decision
Use GitHub Actions for all CI/CD workflows with three separate pipeline definitions:
- Pull Request Validation (pull-request.yml)
- CI Pipeline (ci.yml)
- CD Pipeline (cd.yml)

## Consequences

### Positive
- Native GitHub integration
- Free for public repositories
- Extensive marketplace for actions
- YAML-based configuration (version controlled)
- Matrix builds for cross-platform testing

### Negative
- Vendor lock-in to GitHub
- Limited free minutes for private repos
- Complex workflows can be hard to debug

## Alternatives Considered
1. **Azure DevOps Pipelines**: Better for enterprise/Azure-heavy shops
2. **Jenkins**: More flexible but requires infrastructure management
3. **CircleCI**: Good UX but limited free tier
