# Architecture Diagrams

This directory contains architecture diagrams for all portfolio projects. Diagrams are authored in [Mermaid](https://mermaid-js.github.io/) format for version control and rendering.

## Diagrams

### [clean-architecture.md](clean-architecture.md)
Clean Architecture dependency flow for the Product Catalog API.

### [microservices.md](microservices.md)
Microservices topology and Saga event flow.

### [cicd.md](cicd.md)
CI/CD pipeline flow for GitHub Actions and Azure DevOps.

## Rendering

These `.md` files contain Mermaid code blocks that render automatically on:
- GitHub (native support)
- VS Code (with Mermaid extension)
- [Mermaid Live Editor](https://mermaid.live)

To export as PNG/SVG:
1. Open in Mermaid Live Editor
2. Click "Actions" → "Download"
3. Save as `clean-architecture.png`, etc.
