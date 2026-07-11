# Event Hub

A content-driven event management platform demonstrating enterprise .NET + Angular full-stack engineering: event/venue content modeling, REST + GraphQL APIs, and a responsive booking flow.

## Tech Stack
- **Backend:** ASP.NET Core 8, EF Core, SQL Server, HotChocolate (GraphQL)
- **Frontend:** Angular 21 (standalone components, signals)
- **Auth:** JWT
- **CI/CD:** GitHub Actions

## Project Structure

```
event-hub/
├── CLAUDE.md                          # Project context/instructions for Claude Code
├── README.md                          # This file
├── docs/
│   └── adr/                           # Architecture Decision Records
├── backend/
│   ├── src/
│   │   ├── EventHub.Api/              # Controllers, GraphQL resolvers, middleware, Program.cs
│   │   │   ├── Controllers/
│   │   │   ├── GraphQL/
│   │   │   └── Middleware/
│   │   ├── EventHub.Application/      # Business logic, DTOs, services
│   │   │   ├── Services/
│   │   │   ├── DTOs/
│   │   │   └── Common/                # Result<T> pattern, shared abstractions
│   │   ├── EventHub.Domain/           # Entities, enums, domain logic
│   │   │   ├── Entities/
│   │   │   └── Enums/
│   │   └── EventHub.Infrastructure/    # EF Core, repositories, external integrations
│   │       ├── Persistence/
│   │       │   ├── Configurations/    # IEntityTypeConfiguration<T> classes
│   │       │   └── Migrations/
│   │       └── Repositories/
│   └── tests/
│       ├── EventHub.UnitTests/
│       └── EventHub.IntegrationTests/
└── frontend/
    └── src/
        ├── app/
        │   ├── core/                  # Singleton services, models, guards, interceptors
        │   │   ├── services/
        │   │   └── models/
        │   ├── features/              # Feature modules (standalone components)
        │   │   ├── events/
        │   │   └── bookings/
        │   └── shared/                # Reusable/dumb components
        │       └── components/
        └── environments/
```

## Local Setup

### Prerequisites
- .NET 8 SDK
- Node.js 20+ and npm
- SQL Server (LocalDB, Express, or full instance)
- Angular CLI 21 (`npm install -g @angular/cli@21`)

### Backend
```bash
cd backend
dotnet restore
dotnet ef database update --project src/EventHub.Infrastructure --startup-project src/EventHub.Api
dotnet run --project src/EventHub.Api
```

### Frontend
```bash
cd frontend
npm install
ng serve
```

## Documentation
- See `CLAUDE.md` for full architecture, domain model, and engineering standards this project follows.
- See `docs/adr/` for architecture decision records as they're logged.
