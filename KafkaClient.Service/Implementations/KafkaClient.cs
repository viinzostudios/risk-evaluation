using System;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using KafkaClient.Service.Interfaces;

namespace KafkaClient.Service.Implementations
{
    

    public class KafkaClient : IKafkaClient
    {
        private readonly KafkaSettings _settings;
        private readonly IProducer<string, string> _producer;
        private readonly IConsumer<string, string> _consumer;

        public KafkaClient(IOptions<KafkaSettings> settings)
        {
            _settings = settings.Value;

            // Config Producer
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _settings.BootstrapServers
            };

            _producer = new ProducerBuilder<string, string>(producerConfig).Build();

            // Config Consumer
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _settings.BootstrapServers,
                GroupId = _settings.GroupId,
                AutoOffsetReset = Enum.Parse<AutoOffsetReset>(_settings.AutoOffsetReset, true),
                EnableAutoCommit = _settings.EnableAutoCommit
            };

            _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        }

        // -------- PUBLISH ----------
        public async Task PublishAsync<T>(string topic, T message)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(message);

            await _producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = json
            });
        }

        // -------- SUBSCRIBE (1 Topic) ----------
        public void Subscribe(string topic, Action<string, string> handler)
        {
            Subscribe(new List<string> { topic }, handler);
        }

        // -------- SUBSCRIBE (Multiple Topics) ----------
        public void Subscribe(IEnumerable<string> topics, Action<string, string> handler)
        {
            _consumer.Subscribe(topics);

            Task.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        var cr = _consumer.Consume();

                        handler(cr.Topic, cr.Message.Value);
                    }
                }
                catch (OperationCanceledException)
                {
                    _consumer.Close();
                }
            });
        }
    }

}
