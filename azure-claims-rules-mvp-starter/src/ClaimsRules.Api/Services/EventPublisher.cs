
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Azure;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Configuration;

namespace ClaimsRules.Api.Services
{
    public class EventPublisher
    {
        private readonly EventGridPublisherClient? _client;
        private readonly bool _useMock;
        private readonly List<(string eventType, object payload)> _mockEvents = new();

        public EventPublisher(IConfiguration config)
        {
            _useMock = config["USE_MOCK_SERVICES"] == "true";
            
            if (!_useMock)
            {
                var endpoint = new Uri(config["EventGrid:TopicEndpoint"]!);
                var key = new AzureKeyCredential(config["EventGrid:AccessKey"]!);
                _client = new EventGridPublisherClient(endpoint, key);
            }
        }

        public async Task SendAsync(string eventType, object payload)
        {
            if (_useMock)
            {
                _mockEvents.Add((eventType, payload));
                return;
            }
            
            var e = new EventGridEvent(
                subject: $"claims/{eventType}",
                eventType: eventType,
                dataVersion: "1.0",
                data: payload);
            await _client!.SendEventAsync(e);
        }
        
        public List<(string eventType, object payload)> GetMockEvents() => _mockEvents;
    }
}
