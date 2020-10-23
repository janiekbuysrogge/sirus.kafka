using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Avro;
using Avro.Generic;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace sirus.kafka.producer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            await SendMessageDefault("GET weatherforecast");
            await SendMessageAvro("GET weatherforecast");

            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }

        private async Task SendMessageDefault(string message)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = "kafka:29092",
                ClientId = Dns.GetHostName(),
                
            };

            using (var producer = new ProducerBuilder<Null, string>(config).Build())
            {
                await producer.ProduceAsync("api-methods-requested-default", new Message<Null, string> { Value = message });
            }
        }

        private async Task SendMessageAvro(string message)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = "kafka:29092",
                ClientId = Dns.GetHostName()
            };

            String key = "key1";
            String userSchema = "{\"type\":\"record\"," +
                                "\"name\":\"myrecord\"," +
                                "\"fields\":[{\"name\":\"f1\",\"type\":\"string\"}]}";
            var schema = (RecordSchema)RecordSchema.Parse(userSchema);
            GenericRecord avroRecord = new GenericRecord(schema);
            avroRecord.Add("f1", "value");

            var schemaRegistryUrl = "http://schema-registry:8085";
            using (var schemaRegistry = new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = schemaRegistryUrl }))
            {
                using (var producer = new ProducerBuilder<string, GenericRecord>(config)
                    .SetKeySerializer(new AvroSerializer<string>(schemaRegistry))
                    .SetValueSerializer(new AvroSerializer<GenericRecord>(schemaRegistry))
                    .Build())
                {
                    await producer.ProduceAsync("api-methods-requested-avro",
                        new Message<string, GenericRecord> { Key = Guid.NewGuid().ToString("N"), Value = avroRecord });
                }
            }
        }
    }
}
