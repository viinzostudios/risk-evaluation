# Implementación Sistema de Pagos con Evaluación de Riesgo

## Guía paso a paso para implementar el sistema de transferencias con Clean Architecture

---

## Tabla de Contenidos

1. [Preparación del Entorno](#1-preparación-del-entorno)
2. [Configuración de Infraestructura (Docker)](#2-configuración-de-infraestructura-docker)
3. [Capa de Dominio (Core)](#3-capa-de-dominio-core)
4. [Capa de Aplicación (UseCases)](#4-capa-de-aplicación-usecases)
5. [Capa de Infraestructura](#5-capa-de-infraestructura)
6. [Capa de Presentación (Web/API)](#6-capa-de-presentación-webapi)
7. [Servicio de Evaluación de Riesgo](#7-servicio-de-evaluación-de-riesgo)
8. [Testing](#8-testing)
9. [Ejecución y Pruebas](#9-ejecución-y-pruebas)

---

## 1. Preparación del Entorno

### 1.1 Agregar Paquetes NuGet Necesarios

Editar `Directory.Packages.props` y agregar:

```xml
<!-- Kafka -->
<PackageVersion Include="Confluent.Kafka" Version="2.3.0" />

<!-- Background Services -->
<PackageVersion Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
```

### 1.2 Agregar Referencias a los Proyectos

**Clean.Architecture.Infrastructure.csproj:**
```xml
<ItemGroup>
  <PackageReference Include="Confluent.Kafka" />
  <PackageReference Include="Microsoft.Extensions.Hosting" />
</ItemGroup>
```

---

## 2. Configuración de Infraestructura (Docker)

### 2.1 Crear `docker-compose.yml` en la raíz del proyecto

```yaml
version: '3.8'

services:
  # SQL Server
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: payment-sqlserver
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong@Passw0rd
      - MSSQL_PID=Developer
    ports:
      - "1433:1433"
    volumes:
      - sqlserver-data:/var/opt/mssql
    networks:
      - payment-network

  # Zookeeper (requerido por Kafka)
  zookeeper:
    image: confluentinc/cp-zookeeper:7.5.0
    container_name: payment-zookeeper
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181
      ZOOKEEPER_TICK_TIME: 2000
    ports:
      - "2181:2181"
    networks:
      - payment-network

  # Kafka
  kafka:
    image: confluentinc/cp-kafka:7.5.0
    container_name: payment-kafka
    depends_on:
      - zookeeper
    ports:
      - "9092:9092"
      - "9093:9093"
    environment:
      KAFKA_BROKER_ID: 1
      KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092,PLAINTEXT_INTERNAL://kafka:9093
      KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: PLAINTEXT:PLAINTEXT,PLAINTEXT_INTERNAL:PLAINTEXT
      KAFKA_INTER_BROKER_LISTENER_NAME: PLAINTEXT_INTERNAL
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
      KAFKA_AUTO_CREATE_TOPICS_ENABLE: 'true'
    networks:
      - payment-network

  # Kafka UI (opcional, para debugging)
  kafka-ui:
    image: provectuslabs/kafka-ui:latest
    container_name: payment-kafka-ui
    depends_on:
      - kafka
    ports:
      - "8080:8080"
    environment:
      KAFKA_CLUSTERS_0_NAME: local
      KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS: kafka:9093
    networks:
      - payment-network

volumes:
  sqlserver-data:

networks:
  payment-network:
    driver: bridge
```

### 2.2 Iniciar servicios

```bash
docker-compose up -d
```

### 2.3 Verificar que Kafka está funcionando

```bash
docker exec -it payment-kafka kafka-topics --list --bootstrap-server localhost:9093
```

---

## 3. Capa de Dominio (Core)

### 3.1 Crear el Agregado PaymentOperation

**Ruta:** `src/Clean.Architecture.Core/PaymentAggregate/PaymentOperation.cs`

```csharp
using Ardalis.GuardClauses;
using Ardalis.SharedKernel;
using Clean.Architecture.Core.PaymentAggregate.Events;

namespace Clean.Architecture.Core.PaymentAggregate;

public class PaymentOperation : EntityBase, IAggregateRoot
{
  public Guid ExternalOperationId { get; private set; }
  public Guid CustomerId { get; private set; }
  public Guid ServiceProviderId { get; private set; }
  public int PaymentMethodId { get; private set; }
  public decimal Amount { get; private set; }
  public PaymentStatus Status { get; private set; }
  public DateTime CreatedAt { get; private set; }
  public DateTime? UpdatedAt { get; private set; }

  // Constructor para EF Core
  private PaymentOperation() { }

  public PaymentOperation(
    Guid customerId,
    Guid serviceProviderId,
    int paymentMethodId,
    decimal amount)
  {
    Guard.Against.Default(customerId, nameof(customerId));
    Guard.Against.Default(serviceProviderId, nameof(serviceProviderId));
    Guard.Against.NegativeOrZero(amount, nameof(amount));
    Guard.Against.NegativeOrZero(paymentMethodId, nameof(paymentMethodId));

    ExternalOperationId = Guid.NewGuid();
    CustomerId = customerId;
    ServiceProviderId = serviceProviderId;
    PaymentMethodId = paymentMethodId;
    Amount = amount;
    Status = PaymentStatus.Evaluating;
    CreatedAt = DateTime.UtcNow;

    // Registrar evento de dominio
    var domainEvent = new PaymentCreatedEvent(
      ExternalOperationId,
      CustomerId,
      Amount);
    RegisterDomainEvent(domainEvent);
  }

  public void UpdateStatus(PaymentStatus newStatus)
  {
    Guard.Against.Null(newStatus, nameof(newStatus));

    if (Status == newStatus)
      return;

    Status = newStatus;
    UpdatedAt = DateTime.UtcNow;

    // Registrar evento de dominio
    var domainEvent = new PaymentStatusUpdatedEvent(
      ExternalOperationId,
      newStatus);
    RegisterDomainEvent(domainEvent);
  }

  public bool IsInFinalState() =>
    Status == PaymentStatus.Accepted ||
    Status == PaymentStatus.Denied;
}
```

### 3.2 Crear el Smart Enum PaymentStatus

**Ruta:** `src/Clean.Architecture.Core/PaymentAggregate/PaymentStatus.cs`

```csharp
using Ardalis.SmartEnum;

namespace Clean.Architecture.Core.PaymentAggregate;

public class PaymentStatus : SmartEnum<PaymentStatus>
{
  public static readonly PaymentStatus Evaluating = new(nameof(Evaluating), 1);
  public static readonly PaymentStatus Accepted = new(nameof(Accepted), 2);
  public static readonly PaymentStatus Denied = new(nameof(Denied), 3);

  private PaymentStatus(string name, int value) : base(name, value)
  {
  }
}
```

### 3.3 Crear Eventos de Dominio

**Ruta:** `src/Clean.Architecture.Core/PaymentAggregate/Events/PaymentCreatedEvent.cs`

```csharp
using Ardalis.SharedKernel;

namespace Clean.Architecture.Core.PaymentAggregate.Events;

public class PaymentCreatedEvent : DomainEventBase
{
  public Guid ExternalOperationId { get; }
  public Guid CustomerId { get; }
  public decimal Amount { get; }

  public PaymentCreatedEvent(
    Guid externalOperationId,
    Guid customerId,
    decimal amount)
  {
    ExternalOperationId = externalOperationId;
    CustomerId = customerId;
    Amount = amount;
  }
}
```

**Ruta:** `src/Clean.Architecture.Core/PaymentAggregate/Events/PaymentStatusUpdatedEvent.cs`

```csharp
using Ardalis.SharedKernel;

namespace Clean.Architecture.Core.PaymentAggregate.Events;

public class PaymentStatusUpdatedEvent : DomainEventBase
{
  public Guid ExternalOperationId { get; }
  public PaymentStatus NewStatus { get; }

  public PaymentStatusUpdatedEvent(
    Guid externalOperationId,
    PaymentStatus newStatus)
  {
    ExternalOperationId = externalOperationId;
    NewStatus = newStatus;
  }
}
```

### 3.4 Crear Especificaciones

**Ruta:** `src/Clean.Architecture.Core/PaymentAggregate/Specifications/PaymentByExternalIdSpec.cs`

```csharp
using Ardalis.Specification;

namespace Clean.Architecture.Core.PaymentAggregate.Specifications;

public class PaymentByExternalIdSpec : Specification<PaymentOperation>
{
  public PaymentByExternalIdSpec(Guid externalOperationId)
  {
    Query
      .Where(p => p.ExternalOperationId == externalOperationId);
  }
}
```

**Ruta:** `src/Clean.Architecture.Core/PaymentAggregate/Specifications/PaymentsByCustomerAndDateSpec.cs`

```csharp
using Ardalis.Specification;

namespace Clean.Architecture.Core.PaymentAggregate.Specifications;

public class PaymentsByCustomerAndDateSpec : Specification<PaymentOperation>
{
  public PaymentsByCustomerAndDateSpec(Guid customerId, DateTime date)
  {
    var startOfDay = date.Date;
    var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

    Query
      .Where(p => p.CustomerId == customerId)
      .Where(p => p.CreatedAt >= startOfDay && p.CreatedAt <= endOfDay)
      .Where(p => p.Status == PaymentStatus.Accepted);
  }
}
```

### 3.5 Crear Interfaces de Servicios de Dominio

**Ruta:** `src/Clean.Architecture.Core/Interfaces/IKafkaProducer.cs`

```csharp
namespace Clean.Architecture.Core.Interfaces;

public interface IKafkaProducer
{
  Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default);
}
```

### 3.6 Crear Handler de Evento de Dominio

**Ruta:** `src/Clean.Architecture.Core/PaymentAggregate/Handlers/PaymentCreatedEventHandler.cs`

```csharp
using Ardalis.SharedKernel;
using Clean.Architecture.Core.Interfaces;
using Clean.Architecture.Core.PaymentAggregate.Events;
using Microsoft.Extensions.Logging;

namespace Clean.Architecture.Core.PaymentAggregate.Handlers;

public class PaymentCreatedEventHandler : IHandlesDomainEvent<PaymentCreatedEvent>
{
  private readonly IKafkaProducer _kafkaProducer;
  private readonly ILogger<PaymentCreatedEventHandler> _logger;

  public PaymentCreatedEventHandler(
    IKafkaProducer kafkaProducer,
    ILogger<PaymentCreatedEventHandler> logger)
  {
    _kafkaProducer = kafkaProducer;
    _logger = logger;
  }

  public async Task Handle(PaymentCreatedEvent domainEvent, CancellationToken cancellationToken)
  {
    _logger.LogInformation(
      "Payment created: {ExternalOperationId}. Publishing to Kafka...",
      domainEvent.ExternalOperationId);

    var riskEvaluationRequest = new
    {
      externalOperationId = domainEvent.ExternalOperationId,
      customerId = domainEvent.CustomerId,
      amount = domainEvent.Amount
    };

    await _kafkaProducer.PublishAsync(
      "risk-evaluation-request",
      riskEvaluationRequest,
      cancellationToken);

    _logger.LogInformation(
      "Risk evaluation request published for operation {ExternalOperationId}",
      domainEvent.ExternalOperationId);
  }
}
```

---

## 4. Capa de Aplicación (UseCases)

### 4.1 Crear DTOs

**Ruta:** `src/Clean.Architecture.UseCases/Payments/PaymentOperationDTO.cs`

```csharp
namespace Clean.Architecture.UseCases.Payments;

public record PaymentOperationDTO(
  Guid ExternalOperationId,
  DateTime CreatedAt,
  string Status);
```

### 4.2 Crear Command: CreatePayment

**Ruta:** `src/Clean.Architecture.UseCases/Payments/Create/CreatePaymentCommand.cs`

```csharp
using Ardalis.Result;

namespace Clean.Architecture.UseCases.Payments.Create;

public record CreatePaymentCommand(
  Guid CustomerId,
  Guid ServiceProviderId,
  int PaymentMethodId,
  decimal Amount) : Ardalis.SharedKernel.ICommand<Result<Guid>>;
```

**Ruta:** `src/Clean.Architecture.UseCases/Payments/Create/CreatePaymentHandler.cs`

```csharp
using Ardalis.Result;
using Ardalis.SharedKernel;
using Clean.Architecture.Core.PaymentAggregate;

namespace Clean.Architecture.UseCases.Payments.Create;

public class CreatePaymentHandler : ICommandHandler<CreatePaymentCommand, Result<Guid>>
{
  private readonly IRepository<PaymentOperation> _repository;

  public CreatePaymentHandler(IRepository<PaymentOperation> repository)
  {
    _repository = repository;
  }

  public async Task<Result<Guid>> Handle(
    CreatePaymentCommand request,
    CancellationToken cancellationToken)
  {
    var payment = new PaymentOperation(
      request.CustomerId,
      request.ServiceProviderId,
      request.PaymentMethodId,
      request.Amount);

    var createdPayment = await _repository.AddAsync(payment, cancellationToken);

    return Result<Guid>.Success(createdPayment.ExternalOperationId);
  }
}
```

### 4.3 Crear Query: GetPaymentByExternalId

**Ruta:** `src/Clean.Architecture.UseCases/Payments/Get/GetPaymentByExternalIdQuery.cs`

```csharp
using Ardalis.Result;

namespace Clean.Architecture.UseCases.Payments.Get;

public record GetPaymentByExternalIdQuery(
  Guid ExternalOperationId) : Ardalis.SharedKernel.IQuery<Result<PaymentOperationDTO>>;
```

**Ruta:** `src/Clean.Architecture.UseCases/Payments/Get/GetPaymentByExternalIdHandler.cs`

```csharp
using Ardalis.Result;
using Ardalis.SharedKernel;
using Clean.Architecture.Core.PaymentAggregate;
using Clean.Architecture.Core.PaymentAggregate.Specifications;

namespace Clean.Architecture.UseCases.Payments.Get;

public class GetPaymentByExternalIdHandler :
  IQueryHandler<GetPaymentByExternalIdQuery, Result<PaymentOperationDTO>>
{
  private readonly IReadRepository<PaymentOperation> _repository;

  public GetPaymentByExternalIdHandler(IReadRepository<PaymentOperation> repository)
  {
    _repository = repository;
  }

  public async Task<Result<PaymentOperationDTO>> Handle(
    GetPaymentByExternalIdQuery request,
    CancellationToken cancellationToken)
  {
    var spec = new PaymentByExternalIdSpec(request.ExternalOperationId);
    var payment = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

    if (payment == null)
    {
      return Result<PaymentOperationDTO>.NotFound();
    }

    var dto = new PaymentOperationDTO(
      payment.ExternalOperationId,
      payment.CreatedAt,
      payment.Status.Name.ToLower());

    return Result<PaymentOperationDTO>.Success(dto);
  }
}
```

### 4.4 Crear Command: UpdatePaymentStatus

**Ruta:** `src/Clean.Architecture.UseCases/Payments/UpdateStatus/UpdatePaymentStatusCommand.cs`

```csharp
using Ardalis.Result;

namespace Clean.Architecture.UseCases.Payments.UpdateStatus;

public record UpdatePaymentStatusCommand(
  Guid ExternalOperationId,
  string Status) : Ardalis.SharedKernel.ICommand<Result>;
```

**Ruta:** `src/Clean.Architecture.UseCases/Payments/UpdateStatus/UpdatePaymentStatusHandler.cs`

```csharp
using Ardalis.Result;
using Ardalis.SharedKernel;
using Clean.Architecture.Core.PaymentAggregate;
using Clean.Architecture.Core.PaymentAggregate.Specifications;

namespace Clean.Architecture.UseCases.Payments.UpdateStatus;

public class UpdatePaymentStatusHandler :
  ICommandHandler<UpdatePaymentStatusCommand, Result>
{
  private readonly IRepository<PaymentOperation> _repository;

  public UpdatePaymentStatusHandler(IRepository<PaymentOperation> repository)
  {
    _repository = repository;
  }

  public async Task<Result> Handle(
    UpdatePaymentStatusCommand request,
    CancellationToken cancellationToken)
  {
    var spec = new PaymentByExternalIdSpec(request.ExternalOperationId);
    var payment = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

    if (payment == null)
    {
      return Result.NotFound();
    }

    if (payment.IsInFinalState())
    {
      return Result.Error("Payment is already in a final state");
    }

    var newStatus = PaymentStatus.FromName(
      request.Status,
      ignoreCase: true);

    payment.UpdateStatus(newStatus);

    await _repository.UpdateAsync(payment, cancellationToken);

    return Result.Success();
  }
}
```

---

## 5. Capa de Infraestructura

### 5.1 Configurar Entity Framework

**Ruta:** `src/Clean.Architecture.Infrastructure/Data/Config/PaymentOperationConfiguration.cs`

```csharp
using Clean.Architecture.Core.PaymentAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clean.Architecture.Infrastructure.Data.Config;

public class PaymentOperationConfiguration : IEntityTypeConfiguration<PaymentOperation>
{
  public void Configure(EntityTypeBuilder<PaymentOperation> builder)
  {
    builder.ToTable("PaymentOperations");

    builder.HasKey(p => p.Id);

    builder.Property(p => p.ExternalOperationId)
      .IsRequired()
      .HasMaxLength(36);

    builder.HasIndex(p => p.ExternalOperationId)
      .IsUnique();

    builder.Property(p => p.CustomerId)
      .IsRequired();

    builder.Property(p => p.ServiceProviderId)
      .IsRequired();

    builder.Property(p => p.PaymentMethodId)
      .IsRequired();

    builder.Property(p => p.Amount)
      .IsRequired()
      .HasColumnType("decimal(18,2)");

    builder.Property(p => p.Status)
      .IsRequired()
      .HasConversion(
        s => s.Value,
        v => PaymentStatus.FromValue(v))
      .HasMaxLength(50);

    builder.Property(p => p.CreatedAt)
      .IsRequired();

    builder.Property(p => p.UpdatedAt);
  }
}
```

**Actualizar:** `src/Clean.Architecture.Infrastructure/Data/AppDbContext.cs`

```csharp
// Agregar el DbSet
public DbSet<PaymentOperation> PaymentOperations => Set<PaymentOperation>();
```

### 5.2 Crear Migración

```bash
cd src/Clean.Architecture.Infrastructure
dotnet ef migrations add AddPaymentOperations --startup-project ../Clean.Architecture.Web
```

### 5.3 Implementar Kafka Producer

**Ruta:** `src/Clean.Architecture.Infrastructure/Messaging/KafkaProducer.cs`

```csharp
using System.Text.Json;
using Clean.Architecture.Core.Interfaces;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Clean.Architecture.Infrastructure.Messaging;

public class KafkaProducer : IKafkaProducer, IDisposable
{
  private readonly IProducer<string, string> _producer;
  private readonly ILogger<KafkaProducer> _logger;

  public KafkaProducer(
    IProducer<string, string> producer,
    ILogger<KafkaProducer> logger)
  {
    _producer = producer;
    _logger = logger;
  }

  public async Task PublishAsync<T>(
    string topic,
    T message,
    CancellationToken cancellationToken = default)
  {
    try
    {
      var serializedMessage = JsonSerializer.Serialize(message);

      var kafkaMessage = new Message<string, string>
      {
        Key = Guid.NewGuid().ToString(),
        Value = serializedMessage
      };

      var deliveryResult = await _producer.ProduceAsync(
        topic,
        kafkaMessage,
        cancellationToken);

      _logger.LogInformation(
        "Message published to topic {Topic} at offset {Offset}",
        deliveryResult.Topic,
        deliveryResult.Offset);
    }
    catch (ProduceException<string, string> ex)
    {
      _logger.LogError(ex, "Error publishing message to Kafka topic {Topic}", topic);
      throw;
    }
  }

  public void Dispose()
  {
    _producer?.Dispose();
  }
}
```

### 5.4 Implementar Kafka Consumer Background Service

**Ruta:** `src/Clean.Architecture.Infrastructure/Messaging/RiskEvaluationConsumerService.cs`

```csharp
using System.Text.Json;
using Ardalis.SharedKernel;
using Clean.Architecture.UseCases.Payments.UpdateStatus;
using Confluent.Kafka;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Clean.Architecture.Infrastructure.Messaging;

public class RiskEvaluationConsumerService : BackgroundService
{
  private readonly IConsumer<string, string> _consumer;
  private readonly IServiceScopeFactory _scopeFactory;
  private readonly ILogger<RiskEvaluationConsumerService> _logger;

  public RiskEvaluationConsumerService(
    IConsumer<string, string> consumer,
    IServiceScopeFactory scopeFactory,
    ILogger<RiskEvaluationConsumerService> logger)
  {
    _consumer = consumer;
    _scopeFactory = scopeFactory;
    _logger = logger;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _consumer.Subscribe("risk-evaluation-response");

    _logger.LogInformation("Risk evaluation consumer started");

    try
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        try
        {
          var consumeResult = _consumer.Consume(stoppingToken);

          if (consumeResult?.Message?.Value != null)
          {
            await ProcessMessageAsync(consumeResult.Message.Value, stoppingToken);
            _consumer.Commit(consumeResult);
          }
        }
        catch (ConsumeException ex)
        {
          _logger.LogError(ex, "Error consuming message from Kafka");
        }
      }
    }
    catch (OperationCanceledException)
    {
      _logger.LogInformation("Risk evaluation consumer is stopping");
    }
    finally
    {
      _consumer.Close();
    }
  }

  private async Task ProcessMessageAsync(string messageValue, CancellationToken cancellationToken)
  {
    try
    {
      var response = JsonSerializer.Deserialize<RiskEvaluationResponse>(messageValue);

      if (response == null)
      {
        _logger.LogWarning("Received null or invalid message");
        return;
      }

      _logger.LogInformation(
        "Processing risk evaluation response for operation {ExternalOperationId} with status {Status}",
        response.ExternalOperationId,
        response.Status);

      using var scope = _scopeFactory.CreateScope();
      var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

      var command = new UpdatePaymentStatusCommand(
        response.ExternalOperationId,
        response.Status);

      var result = await mediator.Send(command, cancellationToken);

      if (result.IsSuccess)
      {
        _logger.LogInformation(
          "Payment status updated successfully for operation {ExternalOperationId}",
          response.ExternalOperationId);
      }
      else
      {
        _logger.LogError(
          "Failed to update payment status: {Errors}",
          string.Join(", ", result.Errors));
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error processing Kafka message: {Message}", messageValue);
    }
  }

  private class RiskEvaluationResponse
  {
    public Guid ExternalOperationId { get; set; }
    public string Status { get; set; } = string.Empty;
  }
}
```

### 5.5 Configurar Servicios de Infraestructura

**Actualizar:** `src/Clean.Architecture.Infrastructure/InfrastructureServiceExtensions.cs`

```csharp
using Clean.Architecture.Core.Interfaces;
using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.Infrastructure.Messaging;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clean.Architecture.Infrastructure;

public static class InfrastructureServiceExtensions
{
  public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services,
    IConfiguration configuration)
  {
    // Database
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    services.AddDbContext<AppDbContext>(options =>
      options.UseSqlServer(connectionString));

    services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
    services.AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));

    // Kafka Configuration
    var kafkaConfig = new ProducerConfig
    {
      BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
      Acks = Acks.All,
      MessageTimeoutMs = 5000,
      EnableIdempotence = true
    };

    var consumerConfig = new ConsumerConfig
    {
      BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
      GroupId = "payment-service-group",
      AutoOffsetReset = AutoOffsetReset.Earliest,
      EnableAutoCommit = false
    };

    // Register Kafka Producer
    services.AddSingleton<IProducer<string, string>>(sp =>
      new ProducerBuilder<string, string>(kafkaConfig).Build());

    services.AddSingleton<IKafkaProducer, KafkaProducer>();

    // Register Kafka Consumer
    services.AddSingleton<IConsumer<string, string>>(sp =>
      new ConsumerBuilder<string, string>(consumerConfig).Build());

    // Register Background Service
    services.AddHostedService<RiskEvaluationConsumerService>();

    return services;
  }
}
```

### 5.6 Actualizar appsettings.json

**Ruta:** `src/Clean.Architecture.Web/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=PaymentDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Clean.Architecture.Infrastructure.Messaging": "Debug"
    }
  },
  "AllowedHosts": "*"
}
```

---

## 6. Capa de Presentación (Web/API)

### 6.1 Crear Endpoint: POST /api/payments

**Ruta:** `src/Clean.Architecture.Web/Payments/Create.cs`

```csharp
using Ardalis.Result;
using Clean.Architecture.UseCases.Payments.Create;
using FastEndpoints;
using MediatR;

namespace Clean.Architecture.Web.Payments;

public class Create(IMediator mediator) : Endpoint<CreatePaymentRequest, CreatePaymentResponse>
{
  public override void Configure()
  {
    Post(CreatePaymentRequest.Route);
    AllowAnonymous();
    Summary(s =>
    {
      s.Summary = "Create a new payment operation";
      s.Description = "Creates a payment and sends it for risk evaluation";
      s.ExampleRequest = new CreatePaymentRequest(
        Guid.NewGuid(),
        Guid.NewGuid(),
        2,
        150.00m);
    });
  }

  public override async Task HandleAsync(
    CreatePaymentRequest request,
    CancellationToken cancellationToken)
  {
    var command = new CreatePaymentCommand(
      request.CustomerId,
      request.ServiceProviderId,
      request.PaymentMethodId,
      request.Amount);

    var result = await mediator.Send(command, cancellationToken);

    if (result.Status == ResultStatus.Invalid)
    {
      await SendAsync(
        new CreatePaymentResponse(Guid.Empty, "Invalid request"),
        400,
        cancellationToken);
      return;
    }

    await SendCreatedAtAsync<GetById>(
      new { externalOperationId = result.Value },
      new CreatePaymentResponse(result.Value, "Payment created and sent for evaluation"),
      cancellation: cancellationToken);
  }
}
```

**Ruta:** `src/Clean.Architecture.Web/Payments/Create.CreatePaymentRequest.cs`

```csharp
namespace Clean.Architecture.Web.Payments;

public record CreatePaymentRequest(
  Guid CustomerId,
  Guid ServiceProviderId,
  int PaymentMethodId,
  decimal Amount)
{
  public const string Route = "/api/payments";
}
```

**Ruta:** `src/Clean.Architecture.Web/Payments/Create.CreatePaymentResponse.cs`

```csharp
namespace Clean.Architecture.Web.Payments;

public record CreatePaymentResponse(
  Guid ExternalOperationId,
  string Message);
```

**Ruta:** `src/Clean.Architecture.Web/Payments/Create.CreatePaymentValidator.cs`

```csharp
using FastEndpoints;
using FluentValidation;

namespace Clean.Architecture.Web.Payments;

public class CreatePaymentValidator : Validator<CreatePaymentRequest>
{
  public CreatePaymentValidator()
  {
    RuleFor(x => x.CustomerId)
      .NotEmpty()
      .WithMessage("CustomerId is required");

    RuleFor(x => x.ServiceProviderId)
      .NotEmpty()
      .WithMessage("ServiceProviderId is required");

    RuleFor(x => x.PaymentMethodId)
      .GreaterThan(0)
      .WithMessage("PaymentMethodId must be greater than 0");

    RuleFor(x => x.Amount)
      .GreaterThan(0)
      .WithMessage("Amount must be greater than 0");
  }
}
```

### 6.2 Crear Endpoint: GET /api/payments/{externalOperationId}

**Ruta:** `src/Clean.Architecture.Web/Payments/GetById.cs`

```csharp
using Ardalis.Result;
using Clean.Architecture.UseCases.Payments;
using Clean.Architecture.UseCases.Payments.Get;
using FastEndpoints;
using MediatR;

namespace Clean.Architecture.Web.Payments;

public class GetById(IMediator mediator) : Endpoint<GetPaymentByIdRequest, PaymentOperationDTO>
{
  public override void Configure()
  {
    Get(GetPaymentByIdRequest.Route);
    AllowAnonymous();
    Summary(s =>
    {
      s.Summary = "Get payment operation by external operation ID";
      s.Description = "Returns the current status of a payment operation";
    });
  }

  public override async Task HandleAsync(
    GetPaymentByIdRequest request,
    CancellationToken cancellationToken)
  {
    var query = new GetPaymentByExternalIdQuery(request.ExternalOperationId);
    var result = await mediator.Send(query, cancellationToken);

    if (result.Status == ResultStatus.NotFound)
    {
      await SendNotFoundAsync(cancellationToken);
      return;
    }

    await SendOkAsync(result.Value, cancellationToken);
  }
}
```

**Ruta:** `src/Clean.Architecture.Web/Payments/GetById.GetPaymentByIdRequest.cs`

```csharp
namespace Clean.Architecture.Web.Payments;

public record GetPaymentByIdRequest
{
  public const string Route = "/api/payments/{externalOperationId}";
  public Guid ExternalOperationId { get; init; }
}
```

---

## 7. Servicio de Evaluación de Riesgo

### 7.1 Crear Proyecto de Servicio de Riesgo (Simulador)

**Crear nuevo proyecto:**

```bash
cd src
dotnet new console -n Clean.Architecture.RiskService
cd Clean.Architecture.RiskService
dotnet add package Confluent.Kafka
dotnet add package Microsoft.Extensions.Hosting
dotnet add package Microsoft.Extensions.Logging.Console
```

**Ruta:** `src/Clean.Architecture.RiskService/Program.cs`

```csharp
using Clean.Architecture.RiskService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<RiskEvaluationService>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var host = builder.Build();
await host.RunAsync();
```

**Ruta:** `src/Clean.Architecture.RiskService/RiskEvaluationService.cs`

```csharp
using System.Collections.Concurrent;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Clean.Architecture.RiskService;

public class RiskEvaluationService : BackgroundService
{
  private readonly ILogger<RiskEvaluationService> _logger;
  private readonly IConsumer<string, string> _consumer;
  private readonly IProducer<string, string> _producer;

  // Cache para acumulados diarios por cliente
  private readonly ConcurrentDictionary<string, decimal> _dailyAccumulated = new();

  public RiskEvaluationService(ILogger<RiskEvaluationService> logger)
  {
    _logger = logger;

    var consumerConfig = new ConsumerConfig
    {
      BootstrapServers = "localhost:9092",
      GroupId = "risk-evaluation-service",
      AutoOffsetReset = AutoOffsetReset.Earliest,
      EnableAutoCommit = false
    };

    var producerConfig = new ProducerConfig
    {
      BootstrapServers = "localhost:9092",
      Acks = Acks.All
    };

    _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
    _producer = new ProducerBuilder<string, string>(producerConfig).Build();
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _consumer.Subscribe("risk-evaluation-request");

    _logger.LogInformation("Risk Evaluation Service started");

    try
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        try
        {
          var consumeResult = _consumer.Consume(stoppingToken);

          if (consumeResult?.Message?.Value != null)
          {
            await ProcessRiskEvaluationAsync(consumeResult.Message.Value, stoppingToken);
            _consumer.Commit(consumeResult);
          }
        }
        catch (ConsumeException ex)
        {
          _logger.LogError(ex, "Error consuming message");
        }
      }
    }
    finally
    {
      _consumer.Close();
      _producer.Dispose();
    }
  }

  private async Task ProcessRiskEvaluationAsync(string messageValue, CancellationToken cancellationToken)
  {
    try
    {
      var request = JsonSerializer.Deserialize<RiskEvaluationRequest>(messageValue);

      if (request == null)
      {
        _logger.LogWarning("Received invalid request");
        return;
      }

      _logger.LogInformation(
        "Evaluating risk for operation {ExternalOperationId}, Customer {CustomerId}, Amount {Amount}",
        request.ExternalOperationId,
        request.CustomerId,
        request.Amount);

      // Aplicar reglas de negocio
      var status = EvaluateRisk(request);

      var response = new RiskEvaluationResponse
      {
        ExternalOperationId = request.ExternalOperationId,
        Status = status
      };

      var serializedResponse = JsonSerializer.Serialize(response);

      var message = new Message<string, string>
      {
        Key = request.ExternalOperationId.ToString(),
        Value = serializedResponse
      };

      await _producer.ProduceAsync("risk-evaluation-response", message, cancellationToken);

      _logger.LogInformation(
        "Risk evaluation completed for operation {ExternalOperationId} with status {Status}",
        request.ExternalOperationId,
        status);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error processing risk evaluation");
    }
  }

  private string EvaluateRisk(RiskEvaluationRequest request)
  {
    // Regla 1: Operaciones > 2000 Bs son rechazadas
    if (request.Amount > 2000)
    {
      _logger.LogWarning(
        "Operation {ExternalOperationId} denied: Amount {Amount} exceeds limit of 2000",
        request.ExternalOperationId,
        request.Amount);
      return "denied";
    }

    // Regla 2: Acumulado diario por cliente > 5000 Bs
    var dailyKey = $"{request.CustomerId:N}_{DateTime.UtcNow:yyyy-MM-dd}";
    var currentAccumulated = _dailyAccumulated.GetOrAdd(dailyKey, 0);
    var newAccumulated = currentAccumulated + request.Amount;

    if (newAccumulated > 5000)
    {
      _logger.LogWarning(
        "Operation {ExternalOperationId} denied: Daily accumulated {Accumulated} would exceed limit of 5000",
        request.ExternalOperationId,
        newAccumulated);
      return "denied";
    }

    // Actualizar acumulado
    _dailyAccumulated[dailyKey] = newAccumulated;

    _logger.LogInformation(
      "Operation {ExternalOperationId} accepted. Daily accumulated for customer: {Accumulated}",
      request.ExternalOperationId,
      newAccumulated);

    return "accepted";
  }

  private class RiskEvaluationRequest
  {
    public Guid ExternalOperationId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Amount { get; set; }
  }

  private class RiskEvaluationResponse
  {
    public Guid ExternalOperationId { get; set; }
    public string Status { get; set; } = string.Empty;
  }
}
```

### 7.2 Agregar al Solution

```bash
dotnet sln add src/Clean.Architecture.RiskService/Clean.Architecture.RiskService.csproj
```

---

## 8. Testing

### 8.1 Unit Tests - Domain Logic

**Ruta:** `tests/Clean.Architecture.UnitTests/Core/PaymentAggregate/PaymentOperationTests.cs`

```csharp
using Clean.Architecture.Core.PaymentAggregate;
using FluentAssertions;
using Xunit;

namespace Clean.Architecture.UnitTests.Core.PaymentAggregate;

public class PaymentOperationTests
{
  [Fact]
  public void Constructor_ShouldCreatePaymentWithEvaluatingStatus()
  {
    // Arrange
    var customerId = Guid.NewGuid();
    var serviceProviderId = Guid.NewGuid();
    var paymentMethodId = 1;
    var amount = 100m;

    // Act
    var payment = new PaymentOperation(
      customerId,
      serviceProviderId,
      paymentMethodId,
      amount);

    // Assert
    payment.CustomerId.Should().Be(customerId);
    payment.ServiceProviderId.Should().Be(serviceProviderId);
    payment.PaymentMethodId.Should().Be(paymentMethodId);
    payment.Amount.Should().Be(amount);
    payment.Status.Should().Be(PaymentStatus.Evaluating);
    payment.ExternalOperationId.Should().NotBeEmpty();
    payment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
  }

  [Fact]
  public void UpdateStatus_ShouldUpdateStatusAndUpdatedAt()
  {
    // Arrange
    var payment = new PaymentOperation(
      Guid.NewGuid(),
      Guid.NewGuid(),
      1,
      100m);

    // Act
    payment.UpdateStatus(PaymentStatus.Accepted);

    // Assert
    payment.Status.Should().Be(PaymentStatus.Accepted);
    payment.UpdatedAt.Should().NotBeNull();
    payment.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
  }

  [Fact]
  public void IsInFinalState_ShouldReturnTrueForAcceptedOrDenied()
  {
    // Arrange
    var payment = new PaymentOperation(Guid.NewGuid(), Guid.NewGuid(), 1, 100m);

    // Act & Assert
    payment.IsInFinalState().Should().BeFalse();

    payment.UpdateStatus(PaymentStatus.Accepted);
    payment.IsInFinalState().Should().BeTrue();

    var payment2 = new PaymentOperation(Guid.NewGuid(), Guid.NewGuid(), 1, 100m);
    payment2.UpdateStatus(PaymentStatus.Denied);
    payment2.IsInFinalState().Should().BeTrue();
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-100)]
  public void Constructor_ShouldThrowException_WhenAmountIsInvalid(decimal amount)
  {
    // Act
    var act = () => new PaymentOperation(
      Guid.NewGuid(),
      Guid.NewGuid(),
      1,
      amount);

    // Assert
    act.Should().Throw<ArgumentException>();
  }
}
```

### 8.2 Functional Tests - API Endpoints

**Ruta:** `tests/Clean.Architecture.FunctionalTests/Payments/CreatePaymentTests.cs`

```csharp
using System.Net;
using System.Net.Http.Json;
using Clean.Architecture.Web.Payments;
using FluentAssertions;
using Xunit;

namespace Clean.Architecture.FunctionalTests.Payments;

public class CreatePaymentTests : IClassFixture<CustomWebApplicationFactory>
{
  private readonly HttpClient _client;

  public CreatePaymentTests(CustomWebApplicationFactory factory)
  {
    _client = factory.CreateClient();
  }

  [Fact]
  public async Task CreatePayment_ShouldReturnCreated_WhenRequestIsValid()
  {
    // Arrange
    var request = new CreatePaymentRequest(
      Guid.NewGuid(),
      Guid.NewGuid(),
      2,
      150.00m);

    // Act
    var response = await _client.PostAsJsonAsync("/api/payments", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);

    var createdPayment = await response.Content.ReadFromJsonAsync<CreatePaymentResponse>();
    createdPayment.Should().NotBeNull();
    createdPayment!.ExternalOperationId.Should().NotBeEmpty();
  }

  [Fact]
  public async Task CreatePayment_ShouldReturnBadRequest_WhenAmountIsNegative()
  {
    // Arrange
    var request = new CreatePaymentRequest(
      Guid.NewGuid(),
      Guid.NewGuid(),
      2,
      -50.00m);

    // Act
    var response = await _client.PostAsJsonAsync("/api/payments", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }
}
```

---

## 9. Ejecución y Pruebas

### 9.1 Levantar Infraestructura

```bash
# Iniciar Docker Compose
docker-compose up -d

# Verificar que los servicios estén corriendo
docker ps
```

### 9.2 Aplicar Migraciones

```bash
cd src/Clean.Architecture.Web
dotnet ef database update --project ../Clean.Architecture.Infrastructure
```

### 9.3 Ejecutar Aplicaciones

**Terminal 1 - API Principal:**
```bash
cd src/Clean.Architecture.Web
dotnet run
```

**Terminal 2 - Servicio de Riesgo:**
```bash
cd src/Clean.Architecture.RiskService
dotnet run
```

### 9.4 Probar los Endpoints

**1. Crear una transferencia:**

```bash
curl -X POST http://localhost:5000/api/payments \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "cfe8b150-2f84-4a1a-bdf4-923b20e34973",
    "serviceProviderId": "5fa3ab5c-645f-4cd5-b29e-5c5c116d7ea4",
    "paymentMethodId": 2,
    "amount": 150.00
  }'
```

**Respuesta esperada:**
```json
{
  "externalOperationId": "2a7fb0cd-4c1c-4e6e-b8f9-ef83bb14cf23",
  "message": "Payment created and sent for evaluation"
}
```

**2. Consultar el estado:**

```bash
curl http://localhost:5000/api/payments/2a7fb0cd-4c1c-4e6e-b8f9-ef83bb14cf23
```

**Respuesta esperada:**
```json
{
  "externalOperationId": "2a7fb0cd-4c1c-4e6e-b8f9-ef83bb14cf23",
  "createdAt": "2025-07-17T08:15:30Z",
  "status": "accepted"
}
```

### 9.5 Verificar Kafka (Opcional)

Acceder a Kafka UI: http://localhost:8080

Deberías ver:
- Tópico: `risk-evaluation-request`
- Tópico: `risk-evaluation-response`

### 9.6 Escenarios de Prueba

**Escenario 1: Operación aceptada**
```bash
curl -X POST http://localhost:5000/api/payments \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "cfe8b150-2f84-4a1a-bdf4-923b20e34973",
    "serviceProviderId": "5fa3ab5c-645f-4cd5-b29e-5c5c116d7ea4",
    "paymentMethodId": 2,
    "amount": 100.00
  }'
```

**Escenario 2: Operación rechazada (monto > 2000)**
```bash
curl -X POST http://localhost:5000/api/payments \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "cfe8b150-2f84-4a1a-bdf4-923b20e34973",
    "serviceProviderId": "5fa3ab5c-645f-4cd5-b29e-5c5c116d7ea4",
    "paymentMethodId": 2,
    "amount": 2500.00
  }'
```

**Escenario 3: Exceder límite diario (> 5000)**
Realizar múltiples operaciones con el mismo `customerId` hasta superar 5000 Bs.

---

## Resumen de Implementación

### Estructura Final del Proyecto

```
Clean.Architecture/
├── src/
│   ├── Clean.Architecture.Core/
│   │   └── PaymentAggregate/
│   │       ├── PaymentOperation.cs
│   │       ├── PaymentStatus.cs
│   │       ├── Events/
│   │       ├── Handlers/
│   │       └── Specifications/
│   ├── Clean.Architecture.UseCases/
│   │   └── Payments/
│   │       ├── Create/
│   │       ├── Get/
│   │       └── UpdateStatus/
│   ├── Clean.Architecture.Infrastructure/
│   │   ├── Data/
│   │   │   └── Config/
│   │   └── Messaging/
│   │       ├── KafkaProducer.cs
│   │       └── RiskEvaluationConsumerService.cs
│   ├── Clean.Architecture.Web/
│   │   └── Payments/
│   │       ├── Create.cs
│   │       └── GetById.cs
│   └── Clean.Architecture.RiskService/
│       ├── Program.cs
│       └── RiskEvaluationService.cs
└── docker-compose.yml
```

### Flujo Completo

1. Cliente envía POST /api/payments
2. FastEndpoint valida y crea CreatePaymentCommand
3. Handler crea PaymentOperation (estado: evaluating)
4. Se persiste en SQL Server
5. Se dispara PaymentCreatedEvent
6. EventHandler publica mensaje a Kafka (risk-evaluation-request)
7. RiskService consume mensaje
8. RiskService evalúa reglas de negocio
9. RiskService publica respuesta a Kafka (risk-evaluation-response)
10. API consume respuesta
11. Se actualiza estado de PaymentOperation (accepted/denied)
12. Cliente puede consultar GET /api/payments/{id}

### Tecnologías Utilizadas

- **Clean Architecture**: Separación de capas
- **DDD**: Agregados, eventos de dominio, especificaciones
- **CQRS**: Commands y Queries separados
- **MediatR**: Mediator pattern
- **FastEndpoints**: API endpoints ligeros
- **Entity Framework Core**: ORM
- **Kafka**: Mensajería asíncrona
- **Docker**: Containerización de infraestructura
- **xUnit + FluentAssertions**: Testing

---

## Próximos Pasos Recomendados

1. **Agregar autenticación/autorización** (JWT)
2. **Implementar circuit breaker** para resiliencia
3. **Agregar métricas y health checks**
4. **Configurar logging distribuido** (Serilog + Seq)
5. **Implementar idempotencia** en endpoints
6. **Agregar validaciones de negocio adicionales**
7. **Dockerizar las aplicaciones** .NET
8. **Implementar retry policies** para Kafka
9. **Agregar más tests** (integración, carga)
10. **Configurar CI/CD** pipeline

---

**¡Implementación completa siguiendo Clean Architecture!**
