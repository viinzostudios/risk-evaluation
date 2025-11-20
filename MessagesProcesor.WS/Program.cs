using MessagesProcesor.WS;
using Infrastructure;
using Application;
using KafkaClient.Service;
using KafkaClient.Service.Interfaces;
using kcs = KafkaClient.Service.Implementations;

var builder = Host.CreateApplicationBuilder(args);

// Application Layer (MediatR, AutoMapper, Validators)
builder.Services.AddApplication();

// Infrastructure (DbContext, Repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// Kafka Client
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("KafkaSettings"));
builder.Services.AddSingleton<IKafkaClient, kcs.KafkaClient>();

// Background Services
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
