using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafkaClient.Service.Interfaces
{
    public interface IKafkaClient
    {
        Task PublishAsync<T>(string topic, T message);
        void Subscribe(string topic, Action<string, string> handler);
        void Subscribe(IEnumerable<string> topics, Action<string, string> handler);
    }
}
