# Risk Evaluation Platform

## About

Risk Evaluation Platform is a financial platform for transfer management with automated risk validation. It integrates risk assessment and orchestration for payment operations, providing real-time validation and control over transfer requests.

This project leverages a modern tech stack, including:

- **.NET 8**
- **C#** - main programming language
- **SQL Server** - relational database for transaction persistence
- **Apache Kafka** - distributed event streaming platform for asynchronous messaging
- **Docker** - containerization for Kafka and database services

## Development Requirements

To develop and run this project, you will need:

- **Visual Studio**: Microsoft's IDE for building, debugging, and deploying applications
  - *Version*: Visual Studio 2022 or later (with .NET 8 SDK support).
- **.NET 8 SDK**: Required to build and run the application.
- **Docker Desktop**: For running SQL Server and Kafka containers.
- **NuGet Packages**: Reusable components for .NET projects that are automatically restored during build.

| NuGet Package                                  | Version | Description                                                    |
| ---------------------------------------------- | ------- | -------------------------------------------------------------- |
| MediatR                                        | 13.1.0  | Mediator pattern implementation for CQRS and message handling. |
| AutoMapper                                     | 15.1.0  | Library for automatically mapping objects between models.      |
| FluentValidation                               | 12.1.0  | Validation library for .NET with fluent interface.             |
| FluentValidation.DependencyInjectionExtensions | 12.1.0  | Dependency injection integration for FluentValidation.         |
| Microsoft.EntityFrameworkCore                  | 9.0.11  | Entity Framework Core ORM for data access.                     |
| Microsoft.EntityFrameworkCore.SqlServer        | 9.0.11  | SQL Server database provider for Entity Framework Core.        |
| Microsoft.EntityFrameworkCore.Design           | 9.0.11  | Design-time tools for EF Core (migrations, scaffolding).       |
| Confluent.Kafka                                | 2.12.0  | Apache Kafka client for .NET applications.                     |
| Microsoft.Extensions.Hosting                   | 8.0.1   | Generic host for background services and workers.              |
| Microsoft.Extensions.Options                   | 9.0.11  | Options pattern for configuration management.                  |
| Microsoft.Extensions.Options.ConfigurationExtensions | 10.0.0  | Configuration binding extensions for options pattern.    |

## Running the Project in Development Mode

Follow these steps to run the Risk Evaluation Platform in your local development environment.

### Step 1: Clone the Repository

```bash
git clone https://github.com/viinzostudios/risk-evaluation.git
cd risk-evaluation
```

### Step 2: Start Infrastructure Services with Docker

The project requires SQL Server and Kafka running locally. Use Docker Compose to start these services:

```bash
docker-compose up -d
```

This will start:
- **SQL Server 2022** on port `1433`
- **Zookeeper** on port `2181`
- **Kafka** on port `9092`
- **Kafka UI** (optional) on port `8080`

Verify containers are running:

```bash
docker ps
```
### Step 3: Run the API Project

Start the Payments API:

```bash
cd Payments.API
dotnet run
```

The API will be available at:
- **HTTPS**: `https://localhost:7283`

### Step 7: Run the Background Worker Service

In a separate terminal, start the message processor:

```bash
cd MessagesProcesor.WS
dotnet run
```

This worker service listens to the `risk-evaluation-response` Kafka topic.

**Create a Payment:**

```bash
POST /api/payments
Content-Type: application/json

{
  "customerId": "cfe8b150-2f84-4a1a-bdf4-923b20e34973",
  "serviceProviderId": "5fa3ab5c-645f-4cd5-b29e-5c5c116d7ea4",
  "paymentMethodId": 2,
  "amount": 150.00
}
```

**Get Payment Status:**

```bash
GET /api/payments/{externalOperationId}
```

### Alternative: Run with Visual Studio

1. Open `RiskEvaluation.sln` in Visual Studio 2022
2. Set `Payments.Api` as the startup project
3. Press `F5` or click **Run**

### Troubleshooting

**Docker containers not starting:**
- Ensure Docker Desktop is running
- Check port availability (1433, 9092, 2181)

**Kafka connection errors:**
- Verify Kafka container is running: `docker logs payment-kafka`
- Check `KafkaSettings.BootstrapServers` is set to `localhost:9092`

## Integrations

Risk Evaluation Platform integrates with various systems and services to extend its capabilities, including:

- **Microsoft SQL Server**: Database engine that stores and manages payment transactions, customer data, and operation history.
  - *Version*: SQL Server 2022
  - *Connection*: Azure SQL Database (sql-server-vz-qa.database.windows.net)
  - *Port*: 1433
  - *Database*: risk-evalutation
  - *Authentication*: SQL Authentication
  - *Encryption*: TLS enabled

- **Apache Kafka**: Distributed event streaming platform for asynchronous communication between payment service and risk evaluation service.
  - *Library*: Confluent.Kafka
  - *Version*: 2.12.0
  - *Bootstrap Servers*: localhost:9092
  - *Topics*:
    - `risk-evaluation-request`: Publishes payment requests for risk validation (Producer)
    - `risk-evaluation-response`: Receives risk evaluation results (Consumer)
  - *Protocol*: PLAINTEXT
  - *Consumer Group*: my-consumer-group
  - *Auto Offset Reset*: Earliest

## Repositories

The repository and version control are managed using [GitHub](https://github.com/).

> **Repository name**: `risk-evaluation`
  **Path**: `https://github.com/viinzostudios/risk-evaluation.git`

#### Branching

Version control strategy that organizes development into branches to separate environments.

> **Path**: `https://github.com/viinzostudios/risk-evaluation/branches`

- `main`: Main branch containing the stable version of the project with all integrated features.

## Project Structure Overview

The project is organized following Clean Architecture principles, with clear separation of concerns across distinct layers ensuring maintainability, testability, and scalability.

```
RiskEvaluation/
├── src/
│   ├── API/
│   │   └── Payments.Api/                 (Presentation Layer - REST API)
│   │       ├── Controllers/
│   │       ├── Models/
│   │       └── Properties/
│   │
│   ├── Services/
│   │   └── KafkaClient.Service/          (Kafka Integration)
│   │       ├── Implementations/
│   │       └── Interfaces/
│   │
│   └── WS/
│       └── MessagesProcesor.WS/          (Background Worker Service)
│           └── Properties/
│
├── Application/                          (Application Layer - CQRS)
│   ├── Behaviors/
│   ├── Commands/
│   │   └── CreatePayment/
│   ├── DTOs/
│   ├── Mappings/
│   └── Queries/
│       └── GetPayment/
│
├── Domain/                               (Domain Layer - Core Business)
│   ├── Common/
│   ├── Entities/
│   └── Interfaces/
│
├── Infrastructure/                       (Infrastructure Layer - Data Access)
│   ├── Data/
│   └── Repositories/
│
└── RiskEvaluation.sln
```

Brief description of the main folders and files, outlining their purpose and role in the application.

- `Application/Behaviors/ValidationBehavior.cs`: MediatR pipeline behavior that validates commands and queries using FluentValidation.
- `Application/Commands/`: Contains CQRS command definitions, handlers, and validators for write operations.
- `Application/Queries/`: Contains CQRS query definitions and handlers for read operations.
- `Application/DTOs/`: Data transfer objects used for API responses.
- `Application/Mappings/`: AutoMapper profiles for object-to-object mappings.
- `Domain/Common/`: Shared domain primitives including base entities, exceptions, and result patterns.
- `Domain/Entities/`: Core business entities (Payment, PaymentMethod, PaymentStatus).
- `Domain/Interfaces/`: Repository contracts and abstractions.
- `Infrastructure/Data/AppDbContext.cs`: Entity Framework Core DbContext for database operations.
- `Infrastructure/Repositories/`: Concrete implementations of repository interfaces.
- `KafkaClient.Service/`: Reusable Kafka client library for publishing and consuming messages.
- `MessagesProcesor.WS/Worker.cs`: Background service that consumes Kafka messages for risk evaluation responses.
- `Payments.API/Controllers/PaymentsController.cs`: REST API endpoints for payment operations (POST, GET).
- `Payments.API/appsettings.json`: Configuration for database connection strings and Kafka settings.
- `RiskEvaluation.sln`: Visual Studio solution file containing all projects.

