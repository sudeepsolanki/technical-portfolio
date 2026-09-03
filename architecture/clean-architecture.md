# Clean Architecture Diagram

## Dependency Flow

```mermaid
graph TB
    subgraph External["External"]
        React[React Frontend]
        Docker[Docker]
    end

    subgraph API["API Layer"]
        Controllers[Controllers]
        Middleware[Exception Middleware]
        Program[Program.cs]
    end

    subgraph App["Application Layer"]
        ProductService[Product Service]
        CategoryService[Category Service]
        Validators[FluentValidation]
        DTOs[DTOs / Mappings]
    end

    subgraph Domain["Domain Layer"]
        Product[Product Entity]
        Category[Category Entity]
        Interfaces[IProductRepository]
        Interfaces2[ICategoryRepository]
        Events[Domain Events]
    end

    subgraph Infra["Infrastructure Layer"]
        DbContext[EF Core DbContext]
        ProductRepo[Product Repository]
        CategoryRepo[Category Repository]
        Seed[Database Seeder]
    end

    React --> Controllers
    Controllers --> ProductService
    Controllers --> CategoryService
    ProductService --> Interfaces
    CategoryService --> Interfaces2
    ProductService --> DTOs
    CategoryService --> DTOs
    Validators --> Product
    Validators --> Category
    ProductRepo --> DbContext
    CategoryRepo --> DbContext
    DbContext[(SQLite)]
    Seed --> DbContext

    style Domain fill:#fff3e0
    style App fill:#e3f2fd
    style API fill:#e8f5e9
    style Infra fill:#fce4ec
```

## Key Principles

- **Dependency Inversion**: Domain defines interfaces, Infrastructure implements them
- **Separation of Concerns**: Each layer has a single responsibility
- **Testability**: Business logic is pure and easily unit tested
- **Independence**: Domain layer has zero external dependencies
