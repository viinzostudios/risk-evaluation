using KafkaClient.Service.Interfaces;
using MediatR;
using System.Text.Json;
using Application.DTOs;
using Application.Commands.EvaluateRisk;
using Application.Commands.UpdatePaymentStatus;

namespace MessagesProcesor.WS
{

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IKafkaClient _kafkaClient;
        private readonly IServiceProvider _serviceProvider;

        public Worker(
            ILogger<Worker> logger,
            IKafkaClient kafkaClient,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _kafkaClient = kafkaClient;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Risk Evaluation Worker started at: {Time}", DateTimeOffset.Now);

            // Subscribe to risk-evaluation-request topic
            _kafkaClient.Subscribe("risk-evaluation-request", async (topic, message) =>
            {
                try
                {
                    _logger.LogInformation("Received message from topic {Topic}", topic);

                    var request = JsonSerializer.Deserialize<RiskEvaluationRequest>(message);
                    if (request == null)
                    {
                        _logger.LogWarning("Failed to deserialize message from topic {Topic}", topic);
                        return;
                    }

                    // Create a scope to use scoped services (MediatR, Repositories)
                    using var scope = _serviceProvider.CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    var evaluateCommand = new EvaluateRiskCommand(
                        request.ExternalOperationId,
                        request.CustomerId,
                        request.Amount);

                    var evaluationResult = await mediator.Send(evaluateCommand, CancellationToken.None);

                    if (evaluationResult.IsSuccess)
                    {
                        _logger.LogInformation(
                            "Risk evaluation completed for payment {ExternalOperationId}: {Status}",
                            evaluationResult.Value.ExternalOperationId,
                            evaluationResult.Value.Status);

                        // Step 2: Update payment status in database
                        var updateCommand = new UpdatePaymentStatusCommand(
                            evaluationResult.Value.ExternalOperationId,
                            evaluationResult.Value.Status);

                        var updateResult = await mediator.Send(updateCommand, CancellationToken.None);

                        if (updateResult.IsSuccess)
                        {
                            _logger.LogInformation(
                                "Payment {ExternalOperationId} status updated successfully to {Status}",
                                request.ExternalOperationId,
                                evaluationResult.Value.Status);
                        }
                        else
                        {
                            _logger.LogError(
                                "Failed to update payment {ExternalOperationId} status: {Error}",
                                request.ExternalOperationId,
                                updateResult.Error);
                        }
                    }
                    else
                    {
                        _logger.LogError(
                            "Failed to evaluate risk for payment {ExternalOperationId}: {Error}",
                            request.ExternalOperationId,
                            evaluationResult.Error);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from topic {Topic}", topic);
                }
            });

            _logger.LogInformation("Worker subscribed to topic: risk-evaluation-request");

            // Keep the worker running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }

            _logger.LogInformation("Risk Evaluation Worker stopped at: {Time}", DateTimeOffset.Now);
        }
    }
}
