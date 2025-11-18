# 📋 ESTADO DEL PROYECTO - Sistema de Pagos con Evaluación de Riesgo

**Fecha última actualización:** 2025-11-18
**Sesión:** Configuración inicial y estructura base de Clean Architecture
**Estado general:** 🟡 En desarrollo - Estructura base completada

---

## 🎯 CONTEXTO DEL PROYECTO

### Descripción
Sistema de gestión de transferencias financieras con evaluación de riesgo. Cada transferencia pasa por validación de un servicio de control de riesgo que decide si se acepta o rechaza según reglas de negocio.

### Stack Tecnológico
- **.NET 8**
- **Base de Datos:** Azure SQL Database (en la nube)
- **Patrón:** Clean Architecture
- **ORM:** Entity Framework Core
- **Mediador:** MediatR (CQRS)
- **API:** ASP.NET Core Controllers

### ⚠️ IMPORTANTE: SIN KAFKA NI DOCKER (POR AHORA)
- ❌ **NO** se implementará Kafka en esta fase
- ❌ **NO** se usará Docker en esta fase
- ✅ Se usará servicio de riesgo **placeholder temporal** (simulación síncrona)
- ✅ Se dejará la arquitectura preparada para agregar Kafka después

---

## 🏗️ DECISIONES ARQUITECTÓNICAS

### 1. **Clean Architecture SIN Ardalis**
El documento de referencia (`IMPLEMENTACION_SISTEMA_PAGOS.md`) usa paquetes de Ardalis, pero **decidimos NO usarlos**.

**Alternativas implementadas:**

| Ardalis Package | Nuestra Solución |
|-----------------|------------------|
| `Ardalis.GuardClauses` | **Clase `Ensure`** personalizada (Domain/Common) |
| `Ardalis.Result` | **Clase `Result<T>`** personalizada (Domain/Common) |
| `Ardalis.SmartEnum` | **Enum tradicional** de C# (`PaymentStatus`) |
| `Ardalis.Specification` | **Métodos en Repository** (LINQ directo) |
| `Ardalis.SharedKernel` | **Clase `EntityBase`** personalizada |

### 2. **Validaciones: Doble Capa**

#### **Capa Web (DTOs/Requests):**
- **DataAnnotations** (`System.ComponentModel.DataAnnotations`)
- Valida datos de entrada HTTP
- Ejemplo:
  ```csharp
  [Required]
  [Range(0.01, double.MaxValue)]
  public decimal Amount { get; set; }
  ```

#### **Capa Domain (Entidades):**
- **Clase `Ensure`** (Guard Clauses personalizados)
- Protege invariantes del dominio
- Ejemplo:
  ```csharp
  Ensure.GreaterThanZero(amount, nameof(amount));
  ```

**Flujo:**
```
HTTP Request → DataAnnotations → DTO → Mapper → Domain Entity → Ensure
```

### 3. **PaymentStatus**
- **Enum tradicional** de C# (no SmartEnum)
- Valores: `Evaluating = 1`, `Accepted = 2`, `Denied = 3`

### 4. **Pattern Result<T>**
- Clase personalizada para manejo de errores
- Uso **INTERNO** (Handler → Controller)
- NO es la respuesta HTTP al cliente

### 5. **Repository Pattern**
- Interfaz `IRepository<T>` genérica
- Interfaz `IPaymentRepository` específica con métodos personalizados
- Implementación en Infrastructure con EF Core

### 6. **Entidades Base**
- Clase abstracta `EntityBase` con propiedades comunes
- Todas las entidades heredan: `Id`, `CreatedAt`, `UpdatedAt`

---

## 📁 ESTRUCTURA DE PROYECTOS

### Nombres de Proyectos (YA RENOMBRADOS)
```
Transational.Api.sln
├── Transational.Api.Domain         (Capa de Dominio)
├── Transational.Api.Application    (Capa de Aplicación)
├── Transational.Api.Infrastructure (Capa de Infraestructura)
└── Transational.Api.Web            (Capa de Presentación)
```

### Referencias entre Proyectos
```
Transational.Api.Web
├── → Transational.Api.Application
└── → Transational.Api.Infrastructure

Transational.Api.Application
└── → Transational.Api.Domain

Transational.Api.Infrastructure
└── → Transational.Api.Domain

Transational.Api.Domain
└── (Sin referencias - núcleo independiente)
```

**Estado actual de referencias en .csproj:**
- ✅ Web → Application: **Configurado**
- ✅ Web → Infrastructure: **Configurado**
- ❌ Application → Domain: **FALTA CONFIGURAR**
- ❌ Infrastructure → Domain: **FALTA CONFIGURAR**

---

## 🗄️ BASE DE DATOS

### Configuración
- **Proveedor:** Azure SQL Database
- **Connection String:**
  ```
  Server=sql-server-vz-qa.database.windows.net,1433;Database=risk-evalutation;User Id=admin-vz;Password=STPC.alm2015;
  ```
- **Estado:** Base de datos existe pero **SIN TABLAS**
- **ORM:** Entity Framework Core 8.0.11

### Paquetes NuGet Requeridos (USUARIO DEBE INSTALAR)
```bash
cd Transational.Api.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.11
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.11

cd ../Transational.Api.Web
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.11

cd ../Transational.Api.Application
dotnet add package MediatR --version 12.4.1

cd ../Transational.Api.Web
dotnet add package MediatR --version 12.4.1

# Herramienta global
dotnet tool install --global dotnet-ef
```

---

## ✅ LO QUE YA ESTÁ HECHO

### ✅ Capa Domain (Transational.Api.Domain)

#### 1. **Common/** - Clases base y utilidades
- ✅ `EntityBase.cs` - Clase base para todas las entidades
  - Propiedades: `Id`, `CreatedAt`, `UpdatedAt`
- ✅ `Ensure.cs` - Guard clauses para validaciones de dominio
  - Métodos: `NotNull`, `NotEmpty`, `GreaterThanZero`, etc.
- ✅ `Result.cs` - Patrón Result para manejo de errores
  - `Result` y `Result<T>`
  - Métodos: `Success()`, `Failure()`, `NotFound()`
- ✅ `DomainException.cs` - Excepción para violaciones de reglas de dominio

#### 2. **Enums/** - Enumeraciones
- ✅ `PaymentStatus.cs` - Estados de pago
  - `Evaluating = 1`
  - `Accepted = 2`
  - `Denied = 3`

#### 3. **Entities/** - Entidades de dominio
- ✅ `PaymentOperation.cs` - Agregado raíz
  - Propiedades: `ExternalOperationId`, `CustomerId`, `ServiceProviderId`, `PaymentMethodId`, `Amount`, `Status`
  - Constructor con validaciones `Ensure`
  - Método `UpdateStatus(PaymentStatus newStatus)`
  - Método `IsInFinalState()`

#### 4. **Interfaces/** - Contratos
- ✅ `IRepository<T>.cs` - Repositorio genérico
  - Métodos: `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, `ListAsync`, `SaveChangesAsync`
- ✅ `IPaymentRepository.cs` - Repositorio específico
  - Métodos adicionales: `GetByExternalIdAsync`, `GetByCustomerIdAsync`, `GetDailyTotalByCustomerAsync`

### ✅ Capa Application (Transational.Api.Application)
- ❌ Aún no implementada (pendiente)

### ✅ Capa Infrastructure (Transational.Api.Infrastructure)
- ❌ Aún no implementada (pendiente)

### ✅ Capa Web (Transational.Api.Web)
- ⚠️ Solo tiene código de ejemplo (WeatherForecast) - pendiente reemplazar

---

## ❌ LO QUE FALTA POR HACER

### 📦 PASO 1: Instalar Paquetes NuGet
**Estado:** ⏳ Pendiente (usuario debe ejecutar)

```bash
# Ejecutar desde la raíz del proyecto
cd Transational.Api.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.11
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.11

cd ../Transational.Api.Web
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.11

cd ../Transational.Api.Application
dotnet add package MediatR --version 12.4.1

cd ../Transational.Api.Web
dotnet add package MediatR --version 12.4.1

cd ..
dotnet tool install --global dotnet-ef
```

**Verificar instalación:**
```bash
dotnet ef --version
```

---

### 📦 PASO 2: Configurar Referencias entre Proyectos

#### 2.1 Application → Domain
```bash
cd Transational.Api.Application
dotnet add reference ../Transational.Api.Domain/Transational.Api.Domain.csproj
```

#### 2.2 Infrastructure → Domain
```bash
cd ../Transational.Api.Infrastructure
dotnet add reference ../Transational.Api.Domain/Transational.Api.Domain.csproj
```

#### 2.3 Actualizar archivo .sln
Agregar los 3 proyectos faltantes al archivo `Transational.Api.sln`:
- Transational.Api.Domain
- Transational.Api.Application
- Transational.Api.Infrastructure

**Actualmente solo tiene:** `viinzo.risk.evaluation` (proyecto antiguo)

---

### 📦 PASO 3: Implementar Infrastructure

#### 3.1 Crear DbContext
**Archivo:** `Transational.Api.Infrastructure/Data/AppDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Transational.Api.Domain.Entities;

namespace Transational.Api.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<PaymentOperation> PaymentOperations => Set<PaymentOperation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

#### 3.2 Crear Entity Configuration
**Archivo:** `Transational.Api.Infrastructure/Data/Configurations/PaymentOperationConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transational.Api.Domain.Entities;
using Transational.Api.Domain.Enums;

namespace Transational.Api.Infrastructure.Data.Configurations;

public class PaymentOperationConfiguration : IEntityTypeConfiguration<PaymentOperation>
{
    public void Configure(EntityTypeBuilder<PaymentOperation> builder)
    {
        builder.ToTable("PaymentOperations");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ExternalOperationId)
            .IsRequired();

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
                v => v.ToString(),
                v => Enum.Parse<PaymentStatus>(v));

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt);
    }
}
```

#### 3.3 Implementar Repository
**Archivo:** `Transational.Api.Infrastructure/Repositories/PaymentRepository.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Transational.Api.Domain.Entities;
using Transational.Api.Domain.Enums;
using Transational.Api.Domain.Interfaces;
using Transational.Api.Infrastructure.Data;

namespace Transational.Api.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _context;

    public PaymentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentOperation?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentOperations.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<PaymentOperation?> GetByExternalIdAsync(Guid externalOperationId, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentOperations
            .FirstOrDefaultAsync(p => p.ExternalOperationId == externalOperationId, cancellationToken);
    }

    public async Task<List<PaymentOperation>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentOperations
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetDailyTotalByCustomerAsync(Guid customerId, DateTime date, CancellationToken cancellationToken = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

        return await _context.PaymentOperations
            .Where(p => p.CustomerId == customerId)
            .Where(p => p.CreatedAt >= startOfDay && p.CreatedAt <= endOfDay)
            .Where(p => p.Status == PaymentStatus.Accepted)
            .SumAsync(p => p.Amount, cancellationToken);
    }

    public async Task<PaymentOperation> AddAsync(PaymentOperation entity, CancellationToken cancellationToken = default)
    {
        await _context.PaymentOperations.AddAsync(entity, cancellationToken);
        await SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(PaymentOperation entity, CancellationToken cancellationToken = default)
    {
        _context.PaymentOperations.Update(entity);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(PaymentOperation entity, CancellationToken cancellationToken = default)
    {
        _context.PaymentOperations.Remove(entity);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task<List<PaymentOperation>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PaymentOperations.ToListAsync(cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
```

#### 3.4 Servicio de Riesgo Placeholder
**Archivo:** `Transational.Api.Infrastructure/Services/RiskEvaluationService.cs`

```csharp
using Transational.Api.Domain.Entities;
using Transational.Api.Domain.Enums;
using Transational.Api.Domain.Interfaces;

namespace Transational.Api.Infrastructure.Services;

/// <summary>
/// Placeholder temporal para evaluación de riesgo
/// TODO: Reemplazar con integración Kafka
/// </summary>
public class RiskEvaluationService : IRiskEvaluationService
{
    private readonly IPaymentRepository _repository;

    public RiskEvaluationService(IPaymentRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaymentStatus> EvaluateAsync(PaymentOperation payment, CancellationToken cancellationToken = default)
    {
        // Regla 1: Operaciones > 2000 son rechazadas
        if (payment.Amount > 2000)
            return PaymentStatus.Denied;

        // Regla 2: Acumulado diario por cliente > 5000
        var dailyTotal = await _repository.GetDailyTotalByCustomerAsync(
            payment.CustomerId,
            payment.CreatedAt.Date,
            cancellationToken);

        if (dailyTotal + payment.Amount > 5000)
            return PaymentStatus.Denied;

        return PaymentStatus.Accepted;
    }
}
```

#### 3.5 Crear interfaz IRiskEvaluationService en Domain
**Archivo:** `Transational.Api.Domain/Interfaces/IRiskEvaluationService.cs`

```csharp
using Transational.Api.Domain.Entities;
using Transational.Api.Domain.Enums;

namespace Transational.Api.Domain.Interfaces;

public interface IRiskEvaluationService
{
    Task<PaymentStatus> EvaluateAsync(PaymentOperation payment, CancellationToken cancellationToken = default);
}
```

---

### 📦 PASO 4: Implementar Application Layer (CQRS con MediatR)

#### 4.1 Crear DTOs
**Archivo:** `Transational.Api.Application/DTOs/PaymentResponse.cs`

```csharp
namespace Transational.Api.Application.DTOs;

/// <summary>
/// Response DTO según especificación FeaturesBackend.txt
/// </summary>
public class PaymentResponse
{
    public Guid ExternalOperationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
```

**Archivo:** `Transational.Api.Application/DTOs/CreatePaymentResponse.cs`

```csharp
namespace Transational.Api.Application.DTOs;

public class CreatePaymentResponse
{
    public Guid ExternalOperationId { get; set; }
    public string Message { get; set; } = string.Empty;
}
```

#### 4.2 Command: CreatePayment
**Archivo:** `Transational.Api.Application/Commands/CreatePayment/CreatePaymentCommand.cs`

```csharp
using MediatR;
using Transational.Api.Application.DTOs;
using Transational.Api.Domain.Common;

namespace Transational.Api.Application.Commands.CreatePayment;

public record CreatePaymentCommand(
    Guid CustomerId,
    Guid ServiceProviderId,
    int PaymentMethodId,
    decimal Amount) : IRequest<Result<CreatePaymentResponse>>;
```

**Archivo:** `Transational.Api.Application/Commands/CreatePayment/CreatePaymentHandler.cs`

```csharp
using MediatR;
using Transational.Api.Application.DTOs;
using Transational.Api.Domain.Common;
using Transational.Api.Domain.Entities;
using Transational.Api.Domain.Interfaces;

namespace Transational.Api.Application.Commands.CreatePayment;

public class CreatePaymentHandler : IRequestHandler<CreatePaymentCommand, Result<CreatePaymentResponse>>
{
    private readonly IPaymentRepository _repository;
    private readonly IRiskEvaluationService _riskService;

    public CreatePaymentHandler(
        IPaymentRepository repository,
        IRiskEvaluationService riskService)
    {
        _repository = repository;
        _riskService = riskService;
    }

    public async Task<Result<CreatePaymentResponse>> Handle(
        CreatePaymentCommand request,
        CancellationToken cancellationToken)
    {
        // Crear la operación de pago
        var payment = new PaymentOperation(
            request.CustomerId,
            request.ServiceProviderId,
            request.PaymentMethodId,
            request.Amount);

        // Guardar con estado Evaluating
        await _repository.AddAsync(payment, cancellationToken);

        // Evaluar riesgo (placeholder - en futuro será Kafka)
        var evaluationResult = await _riskService.EvaluateAsync(payment, cancellationToken);

        // Actualizar estado
        payment.UpdateStatus(evaluationResult);
        await _repository.UpdateAsync(payment, cancellationToken);

        var response = new CreatePaymentResponse
        {
            ExternalOperationId = payment.ExternalOperationId,
            Message = "Payment created and evaluated"
        };

        return Result.Success(response);
    }
}
```

#### 4.3 Query: GetPayment
**Archivo:** `Transational.Api.Application/Queries/GetPayment/GetPaymentQuery.cs`

```csharp
using MediatR;
using Transational.Api.Application.DTOs;
using Transational.Api.Domain.Common;

namespace Transational.Api.Application.Queries.GetPayment;

public record GetPaymentQuery(Guid ExternalOperationId) : IRequest<Result<PaymentResponse>>;
```

**Archivo:** `Transational.Api.Application/Queries/GetPayment/GetPaymentHandler.cs`

```csharp
using MediatR;
using Transational.Api.Application.DTOs;
using Transational.Api.Domain.Common;
using Transational.Api.Domain.Interfaces;

namespace Transational.Api.Application.Queries.GetPayment;

public class GetPaymentHandler : IRequestHandler<GetPaymentQuery, Result<PaymentResponse>>
{
    private readonly IPaymentRepository _repository;

    public GetPaymentHandler(IPaymentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PaymentResponse>> Handle(
        GetPaymentQuery request,
        CancellationToken cancellationToken)
    {
        var payment = await _repository.GetByExternalIdAsync(request.ExternalOperationId, cancellationToken);

        if (payment == null)
            return Result<PaymentResponse>.NotFound("Payment not found");

        var response = new PaymentResponse
        {
            ExternalOperationId = payment.ExternalOperationId,
            CreatedAt = payment.CreatedAt,
            Status = payment.Status.ToString().ToLower()
        };

        return Result.Success(response);
    }
}
```

---

### 📦 PASO 5: Implementar Web Layer (Controllers)

#### 5.1 Crear Request DTOs con DataAnnotations
**Archivo:** `Transational.Api.Web/Models/CreatePaymentRequest.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace Transational.Api.Web.Models;

/// <summary>
/// Request según especificación FeaturesBackend.txt
/// </summary>
public class CreatePaymentRequest
{
    [Required(ErrorMessage = "CustomerId is required")]
    public Guid CustomerId { get; set; }

    [Required(ErrorMessage = "ServiceProviderId is required")]
    public Guid ServiceProviderId { get; set; }

    [Required(ErrorMessage = "PaymentMethodId is required")]
    [Range(1, int.MaxValue, ErrorMessage = "PaymentMethodId must be greater than 0")]
    public int PaymentMethodId { get; set; }

    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
}
```

#### 5.2 Crear Controller
**Archivo:** `Transational.Api.Web/Controllers/PaymentsController.cs`

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Transational.Api.Application.Commands.CreatePayment;
using Transational.Api.Application.Queries.GetPayment;
using Transational.Api.Web.Models;

namespace Transational.Api.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new payment operation
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreatePaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var command = new CreatePaymentCommand(
            request.CustomerId,
            request.ServiceProviderId,
            request.PaymentMethodId,
            request.Amount);

        var result = await _mediator.Send(command);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return CreatedAtAction(
            nameof(GetPayment),
            new { externalOperationId = result.Value.ExternalOperationId },
            result.Value);
    }

    /// <summary>
    /// Get payment status by external operation ID
    /// </summary>
    [HttpGet("{externalOperationId}")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayment(Guid externalOperationId)
    {
        var query = new GetPaymentQuery(externalOperationId);
        var result = await _mediator.Send(query);

        if (result.IsFailure)
            return NotFound(result.Error);

        return Ok(result.Value);
    }
}
```

#### 5.3 Eliminar archivos de ejemplo
- Eliminar `WeatherForecastController.cs`
- Eliminar `WeatherForecast.cs`

---

### 📦 PASO 6: Configurar Dependency Injection

#### 6.1 Actualizar appsettings.json
**Archivo:** `Transational.Api.Web/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sql-server-vz-qa.database.windows.net,1433;Database=risk-evalutation;User Id=admin-vz;Password=STPC.alm2015;TrustServerCertificate=True;Encrypt=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

#### 6.2 Configurar Program.cs
**Archivo:** `Transational.Api.Web/Program.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Transational.Api.Domain.Interfaces;
using Transational.Api.Infrastructure.Data;
using Transational.Api.Infrastructure.Repositories;
using Transational.Api.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

// Services
builder.Services.AddScoped<IRiskEvaluationService, RiskEvaluationService>();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Transational.Api.Application.AssemblyReference).Assembly));

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

#### 6.3 Crear Assembly Reference en Application
**Archivo:** `Transational.Api.Application/AssemblyReference.cs`

```csharp
namespace Transational.Api.Application;

public class AssemblyReference
{
}
```

---

### 📦 PASO 7: Crear y Aplicar Migraciones

```bash
# Desde la raíz del proyecto
cd Transational.Api.Web

# Crear migración
dotnet ef migrations add InitialCreate --project ../Transational.Api.Infrastructure --startup-project .

# Aplicar migración a la base de datos
dotnet ef database update --project ../Transational.Api.Infrastructure --startup-project .
```

---

### 📦 PASO 8: Probar la API

#### Ejecutar la aplicación
```bash
cd Transational.Api.Web
dotnet run
```

#### Probar con Postman

**1. Crear Payment:**
```
POST https://localhost:7170/api/payments
Content-Type: application/json

{
  "customerId": "cfe8b150-2f84-4a1a-bdf4-923b20e34973",
  "serviceProviderId": "5fa3ab5c-645f-4cd5-b29e-5c5c116d7ea4",
  "paymentMethodId": 2,
  "amount": 150.00
}
```

**Respuesta esperada:**
```json
{
  "externalOperationId": "2a7fb0cd-4c1c-4e6e-b8f9-ef83bb14cf23",
  "message": "Payment created and evaluated"
}
```

**2. Obtener Payment:**
```
GET https://localhost:7170/api/payments/2a7fb0cd-4c1c-4e6e-b8f9-ef83bb14cf23
```

**Respuesta esperada:**
```json
{
  "externalOperationId": "2a7fb0cd-4c1c-4e6e-b8f9-ef83bb14cf23",
  "createdAt": "2025-11-18T15:30:00Z",
  "status": "accepted"
}
```

---

## 🎯 REGLAS DE NEGOCIO IMPLEMENTADAS

### Evaluación de Riesgo (Placeholder)
1. **Monto máximo:** Operaciones con `amount > 2000` → `Denied`
2. **Límite diario:** Si el acumulado diario del cliente > 5000 → `Denied`
3. **Por defecto:** Si no se cumple ninguna regla → `Accepted`

### Estados del Pago
- **Evaluating:** Estado inicial al crear el pago
- **Accepted:** Aprobado por evaluación de riesgo
- **Denied:** Rechazado por evaluación de riesgo

---

## 📝 NOTAS IMPORTANTES

### Diferencias con el documento IMPLEMENTACION_SISTEMA_PAGOS.md
1. ❌ **Sin Kafka:** Se usa servicio síncrono placeholder
2. ❌ **Sin Docker:** Base de datos en Azure, no local
3. ❌ **Sin Ardalis:** Implementación personalizada
4. ❌ **Sin FastEndpoints:** Controllers tradicionales de ASP.NET Core
5. ✅ **Con MediatR:** Patrón CQRS implementado
6. ✅ **Con Clean Architecture:** Estructura de capas respetada

### Preparación para Kafka (futuro)
- La arquitectura está lista para agregar Kafka
- Solo hay que:
  1. Reemplazar `RiskEvaluationService` con producer/consumer Kafka
  2. Separar la evaluación de riesgo del proceso de creación
  3. Implementar consumer background service

---

## 🔄 PRÓXIMA SESIÓN - COMANDOS RÁPIDOS

### Para continuar donde lo dejamos:
```bash
# 1. Verificar EF está instalado
dotnet ef --version

# 2. Verificar estructura de archivos
tree Transational.Api.Domain -L 2

# 3. Ver estado de migraciones
cd Transational.Api.Web
dotnet ef migrations list --project ../Transational.Api.Infrastructure

# 4. Ejecutar la aplicación
dotnet run
```

### Checklist rápido:
- [ ] ¿Instalaste los paquetes NuGet?
- [ ] ¿Configuraste las referencias entre proyectos?
- [ ] ¿Actualizaste el archivo .sln?
- [ ] ¿Creaste DbContext y configuraciones?
- [ ] ¿Implementaste repositories?
- [ ] ¿Creaste commands y queries?
- [ ] ¿Configuraste DI en Program.cs?
- [ ] ¿Creaste y aplicaste migraciones?
- [ ] ¿Probaste los endpoints en Postman?

---

## 📚 ARCHIVOS DE REFERENCIA

- `FeaturesBackend.txt` - Requisitos funcionales originales
- `IMPLEMENTACION_SISTEMA_PAGOS.md` - Guía de implementación (con Ardalis y Kafka)
- Este archivo - Estado actual y próximos pasos

---

**¡Listo para continuar en la próxima sesión!** 🚀
