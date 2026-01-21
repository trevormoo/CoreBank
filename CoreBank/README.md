# CoreBank API

A production-grade Banking/Fintech REST API built with .NET 8 and Clean Architecture.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker&logoColor=white)
![Tests](https://img.shields.io/badge/Tests-72%20Passing-brightgreen)

## Features

- **Authentication & Authorization** - JWT-based authentication with role-based access control
- **Account Management** - Create and manage Savings/Checking accounts
- **Transaction Processing** - Deposits, withdrawals, and transfers with idempotency support
- **KYC Verification** - Document submission and admin review workflow
- **Fraud Detection** - Velocity checks, unusual amount detection, suspicious pattern analysis
- **Transaction Limits** - Configurable daily/monthly limits with real-time enforcement
- **Scheduled Payments** - Recurring transfers powered by Hangfire background jobs
- **PDF Statements** - Generate account statements with QuestPDF
- **Audit Logging** - Automatic tracking of all operations via MediatR pipeline
- **Rate Limiting** - IP-based request throttling
- **Health Checks** - PostgreSQL connectivity monitoring

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                    CoreBank.Api                      │
│           Controllers, Middleware, Swagger           │
├─────────────────────────────────────────────────────┤
│                CoreBank.Application                  │
│        CQRS Commands/Queries, Validators, DTOs       │
├─────────────────────────────────────────────────────┤
│               CoreBank.Infrastructure                │
│      EF Core, JWT Auth, Services, Background Jobs    │
├─────────────────────────────────────────────────────┤
│                  CoreBank.Domain                     │
│          Entities, Value Objects, Enums              │
└─────────────────────────────────────────────────────┘
```

## Tech Stack

| Category | Technologies |
|----------|-------------|
| **Framework** | .NET 8, ASP.NET Core |
| **Database** | PostgreSQL 16, Entity Framework Core |
| **Architecture** | Clean Architecture, CQRS, MediatR |
| **Authentication** | JWT Bearer Tokens, BCrypt |
| **Validation** | FluentValidation |
| **Documentation** | Swagger/OpenAPI |
| **Background Jobs** | Hangfire |
| **PDF Generation** | QuestPDF |
| **Logging** | Serilog |
| **Testing** | xUnit, Moq, FluentAssertions |
| **Containerization** | Docker, Docker Compose |

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/get-started) (optional)
- [PostgreSQL 16](https://www.postgresql.org/download/) (or use Docker)

### Run with Docker (Recommended)

```bash
# Clone the repository
git clone https://github.com/trevormoo/CoreBank.git
cd CoreBank

# Start all services
docker-compose up -d

# API available at http://localhost:5000
# Swagger UI at http://localhost:5000
# pgAdmin at http://localhost:5050 (admin@corebank.com / admin)
```

### Run Locally

```bash
# Clone and navigate
git clone https://github.com/trevormoo/CoreBank.git
cd CoreBank

# Start PostgreSQL (using Docker)
docker run -d --name corebank-db \
  -e POSTGRES_USER=corebank \
  -e POSTGRES_PASSWORD=corebank_password \
  -e POSTGRES_DB=corebank \
  -p 5432:5432 \
  postgres:16-alpine

# Run the API
cd src/CoreBank.Api
dotnet run

# API available at http://localhost:5000
```

### Run Tests

```bash
# Run all 72 tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## API Endpoints

### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/auth/register` | Register new user |
| POST | `/api/v1/auth/login` | Login and get JWT token |
| POST | `/api/v1/auth/verify-email` | Verify email address |

### Accounts
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/accounts` | Create new account |
| GET | `/api/v1/accounts` | Get user's accounts |
| GET | `/api/v1/accounts/{id}` | Get account by ID |

### Transactions
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/transactions/deposit` | Deposit funds |
| POST | `/api/v1/transactions/withdraw` | Withdraw funds |
| POST | `/api/v1/transactions/transfer` | Transfer between accounts |
| GET | `/api/v1/transactions/history` | Get transaction history |

### KYC
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/kyc/submit` | Submit KYC document |
| GET | `/api/v1/kyc/documents` | Get user's KYC documents |
| POST | `/api/v1/kyc/review` | Review KYC (Admin) |

### Scheduled Payments
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/scheduled-payments` | Create scheduled payment |
| GET | `/api/v1/scheduled-payments` | Get user's scheduled payments |
| DELETE | `/api/v1/scheduled-payments/{id}` | Cancel scheduled payment |

### Statements
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/statements/{accountId}` | Generate PDF statement |

## Project Structure

```
CoreBank/
├── src/
│   ├── CoreBank.Api/           # Web API layer
│   │   ├── Controllers/        # API endpoints
│   │   ├── Middleware/         # Exception handling, logging
│   │   └── Services/           # API-specific services
│   ├── CoreBank.Application/   # Business logic layer
│   │   ├── Common/             # Shared interfaces, behaviors
│   │   ├── Accounts/           # Account commands/queries
│   │   ├── Transactions/       # Transaction commands/queries
│   │   ├── Users/              # User commands/queries
│   │   └── Kyc/                # KYC commands/queries
│   ├── CoreBank.Domain/        # Domain layer
│   │   ├── Entities/           # Domain entities
│   │   ├── ValueObjects/       # Value objects (Money, Email)
│   │   ├── Enums/              # Domain enums
│   │   └── Exceptions/         # Domain exceptions
│   └── CoreBank.Infrastructure/# Infrastructure layer
│       ├── Persistence/        # EF Core, DbContext
│       ├── Services/           # External service implementations
│       └── BackgroundJobs/     # Hangfire jobs
├── tests/
│   ├── CoreBank.Domain.Tests/      # Domain unit tests
│   ├── CoreBank.Application.Tests/ # Handler unit tests
│   └── CoreBank.Api.Tests/         # Integration tests
├── Dockerfile
├── docker-compose.yml
└── CoreBank.sln
```

## Security Features

- **Password Hashing** - BCrypt with configurable work factor
- **JWT Authentication** - Secure token-based auth with configurable expiration
- **Rate Limiting** - Prevent brute force and DDoS attacks
- **Transaction Limits** - Daily/monthly limits per account type
- **Fraud Detection** - Real-time velocity checks and pattern analysis
- **Audit Logging** - Complete audit trail for compliance
- **Input Validation** - FluentValidation on all requests
- **Non-root Docker** - Container runs as non-privileged user

## License

This project is licensed under the MIT License.

## Author

**Kehinde Akinbi** - [GitHub](https://github.com/trevormoo)
