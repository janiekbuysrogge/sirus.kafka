using System;
using System.Threading.Tasks;
using Avro.Generic;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;

namespace sirus.kafka.consumer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Listening for messages");

            Task.Factory.StartNew(() =>
            {
                var config = new ConsumerConfig
                {
                    GroupId = "request-consumers",
                    BootstrapServers = "kafka:29092",
                    AutoOffsetReset = AutoOffsetReset.Earliest
                };

                using (var consumer = new ConsumerBuilder<Ignore, string>(config).Build())
                {
                    consumer.Subscribe("api-methods-requested-default");
                    while (true)
                    {
                        var result = consumer.Consume();
                        Console.WriteLine("Message received (default): " + result.Message.Value);
                    }
                }
            });

            Task.Factory.StartNew(() =>
            {
                var config = new ConsumerConfig
                {
                    GroupId = "request-consumers",
                    BootstrapServers = "kafka:29092",
                    AutoOffsetReset = AutoOffsetReset.Earliest
                };

                var schemaRegistryUrl = "http://schema-registry:8085";
                using (var schemaRegistry = new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = schemaRegistryUrl }))
                using (var consumer = new ConsumerBuilder<string, GenericRecord>(config)
                    .SetKeyDeserializer(new AvroDeserializer<string>(schemaRegistry).AsSyncOverAsync())
                    .SetValueDeserializer(new AvroDeserializer<GenericRecord>(schemaRegistry).AsSyncOverAsync())
                    .SetErrorHandler((_, e) => Console.WriteLine($"Error: {e.Reason}"))
                    .Build())
                {
                    consumer.Subscribe("api-methods-requested-avro");
                    while (true)
                    {
                        var result = consumer.Consume();
                        Console.WriteLine("Message received (avro): " + result.Message.Value);
                    }
                }
            });

            Console.ReadLine();
            Console.WriteLine("Exiting");
        }
    }
}
