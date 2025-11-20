USE [risk-evalutation];
GO

    -- Tabla PaymentStatus
    CREATE TABLE PaymentStatus (
        Id INT PRIMARY KEY,
        Name NVARCHAR(50) NOT NULL
    );

    -- Tabla PaymentMethods
    CREATE TABLE PaymentMethods (
        Id INT PRIMARY KEY,
        Name NVARCHAR(50) NOT NULL
    );

    -- Tabla Payments
    CREATE TABLE Payments (
            [Id] INT IDENTITY(1,1) NOT NULL,
            [ExternalOperationId] BINARY(16) NOT NULL,
            [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_Payments_CreatedAt] DEFAULT (GETUTCDATE()),
            [UpdatedAt] DATETIME2(7) NULL,
            [CustomerId] BINARY(16) NOT NULL,
            [ServiceProviderId] BINARY(16) NOT NULL,
            [Amount] DECIMAL(18,2) NOT NULL,
            [PaymentMethodId] INT NOT NULL,
            [PaymentStatusId] INT NOT NULL CONSTRAINT [DF_Payments_PaymentStatusId] DEFAULT (1),
            
            CONSTRAINT [PK_Payments] PRIMARY KEY CLUSTERED (Id ASC),
            CONSTRAINT [FK_Payments_PaymentMethods] FOREIGN KEY ([PaymentMethodId]) REFERENCES [dbo].[PaymentMethods]([Id]),
            CONSTRAINT [FK_Payments_PaymentStatus] FOREIGN KEY ([PaymentStatusId]) REFERENCES [dbo].[PaymentStatus]([Id]),
            CONSTRAINT [UQ_Payments_ExternalOperationId] UNIQUE NONCLUSTERED ([ExternalOperationId] ASC)
    );

    -- Índices
    CREATE INDEX IX_Payments_CustomerId ON Payments(CustomerId);
    CREATE INDEX IX_Payments_CustomerId_CreatedAt ON Payments(CustomerId, CreatedAt);


    PRINT 'Table PaymentOperations created successfully';
GO
