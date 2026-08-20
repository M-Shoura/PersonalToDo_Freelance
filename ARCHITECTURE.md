# Personal Todo Manager - Architecture Overview

This document describes the pragmatic application architecture for the Personal Todo Manager web app. The goal is to separate concerns while keeping the design simple and easy to evolve.

Principles
- Keep a single web project (no multi-project complexity) unless the app grows large enough to justify split projects.
- Clear folders that express intent: Domain, Application, Infrastructure, UI (Controllers/Views).
- Favor composition over inheritance; rely on DI for testability.

Folder / Project Organization
- /Domain
  - Entities (domain entities and simple value objects)
  - Exceptions (domain-specific exceptions)
- /Application
  - Interfaces (service contracts used by controllers and other layers)
  - DTOs / ViewModels (objects shaped for MVC views and APIs)
  - Validation (validators or rules)
  - Services (application-level orchestration if needed)
- /Infrastructure
  - Data (EF Core DbContext, migrations)
  - Services (implementations of application interfaces, e.g., current user)
  - Middleware (cross-cutting middleware like error logging)
- /Controllers, /Views, /wwwroot — MVC presentation

Dependency Injection conventions
- Register services by feature area and lifetime in Infrastructure/DependencyInjection.cs and Application/DependencyInjection.cs extension methods.
- Conventions:
  - Stateless services: AddTransient
  - Scoped services that access DbContext or HttpContext: AddScoped
  - Long-lived singletons without context: AddSingleton

Domain entity conventions
- Entities live under /Domain/Entities and have a small base class:
  - Id (long or Guid) — prefer long int for DB identity unless explicit GUID requirement
  - CreatedAt / ModifiedAt timestamps on base entity
- Keep business rules on domain entities where they are simple. For complex orchestration use application services.

Service conventions
- Define interfaces under Application/Interfaces. Keep method signatures returning Task or Task<T> for async DB operations.
- Implementation goes under Infrastructure/Services and only depends on abstractions + EF Core DbContext.

Validation conventions
- Use a validation library (e.g., FluentValidation) later. For now, validators live under Application/Validation and implement IValidator<T>.
- Controllers validate incoming ViewModels and call application services with validated data.

ViewModel conventions
- View models live under Application/ViewModels (or Views/Models) and are shaped for the UI.
- Keep mapping logic in small helper mappers or later use AutoMapper.

Error handling conventions
- Use built-in UseExceptionHandler for friendly error pages in Production.
- Add a small Infrastructure middleware to log unhandled exceptions for diagnostics (uses ILogger).
- Controllers and services should throw typed exceptions for known error cases; middleware maps to status codes.

Logging conventions
- Configure logging in Program.cs using configuration (appsettings). Start with Console and Framework providers.
- Use ILogger<T> injected into services and controllers. Log structured information with EventId where helpful.

Compatibility notes
- Designed to work with EF Core, SQL Server, ASP.NET Core Identity, and MVC.
- Domain design keeps recurring tasks and occurrences in mind; those will be implemented as domain models that reference recurrence patterns and occurrences.

DI registration helpers and skeletons are present in the project to follow these conventions.
