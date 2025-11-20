using Application.Commands.EvaluateRisk;
using Application.Commands.UpdatePaymentStatus;
using Application.DTOs;
using Infrastructure.WS.Interfaces;
using KafkaClient.Service.Interfaces;
using MediatR;
using System.Net;
using System.Text.Json;
using System.Threading;

namespace MessagesProcesor.WS
{

    public class Worker : BackgroundService
    {
        private readonly IPaymentEvaluator _paymentEvaluator;
        public Worker(IPaymentEvaluator paymentEvaluator)
        {
            _paymentEvaluator = paymentEvaluator;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _paymentEvaluator.InitProcess();

            // Keep the worker running
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!_paymentEvaluator.InProgress)
                {
                    _paymentEvaluator.InitProcess();
                }

                await Task.Delay(10000, stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _paymentEvaluator.StopProcess();
            await base.StopAsync(cancellationToken);
        }
    }
}
