using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using SprintsASP_NetCore_API.Infrastructure.DI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace SprintsASP_NetCore_API.Infrastructure.DataAccess.Configurations.BrokerConfigurations
{
    internal class ProducerConfig_Kafka
    {


        async Task S()
        {
            var config = new ProducerConfig
            {
                BootstrapServers = "localhost:9092",
                Acks = Acks.All,
                
            };

            using var producer = new ProducerBuilder<string, string>(config).Build();
             
            var result = await producer.ProduceAsync("orders", new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = JsonSerializer.Serialize("orderCreated")
            }); 
            
            // producer.Flush(TimeSpan.FromSeconds(10)); - только без асинхронной функции 

            Console.WriteLine($"Доставлено: {result.TopicPartitionOffset}");

            var configConsumer  = new ConsumerConfig
           {
               BootstrapServers = "localhost:9092",
               GroupId = "order-processing",
               AutoOffsetReset = AutoOffsetReset.Earliest,
               EnableAutoCommit = false,
               EnableAutoOffsetStore = false // Отключаем автосохранение офсета
           };

            using var consumer = new ConsumerBuilder<string, string>(configConsumer).Build();

            consumer.Subscribe("orders");
            CancellationToken  cancellationToken = default;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(cancellationToken);

                        // Обработка сообщения
                        Console.WriteLine($"[{consumeResult.TopicPartitionOffset}] " +
                                          $"Key: {consumeResult.Message.Key} " +
                                          $"Value: {consumeResult.Message.Value}");

                        // Сохраняем офсет локально в буфер клиента — это ещё не коммит в Kafka
                        // При EnableAutoCommit = true фоновый поток периодически отправит его брокеру
                        // При EnableAutoCommit = false нужно вызвать Commit() явно 
                        consumer.StoreOffset(consumeResult);


                        // Idempotency key 
                        // Уникальный идентификатор события идемпотентность
                        // // Вариант 1 — upsert через EF Core (AddOrUpdate)
                        // var existing = await dbContext.Orders.FindAsync(order.OrderId);
                        // if (existing is null)
                        // {
                        // dbContext.Orders.Add(new OrderEntity { Id = order.OrderId, ... });
                        // await dbContext.SaveChangesAsync();
                        // }
                        // // Если запись уже есть — пропускаем, это повторная доставка

                        // // Вариант 2 — INSERT ... ON CONFLICT DO NOTHING (Dapper + PostgreSQL)
                        // await connection.ExecuteAsync("""
                        //INSERT INTO orders (id, customer_email, total_amount)
                        //VALUES (@Id, @Email, @Amount)
                        //ON CONFLICT (id) DO NOTHING
                        //""",
                        // new { Id = order.OrderId, Email = order.CustomerEmail, Amount = order.TotalAmount });








                    }
                    catch (ConsumeException ex)
                    {
                        Console.WriteLine($"Ошибка при получении сообщения: {ex.Error.Reason}");
                    } 
                } 
            }
            catch (OperationCanceledException)
            {
                // Штатное завершение — CancellationToken был отменён
            }
            finally
            {
                consumer.Close(); // Корректный сигнал “leave group” + коммит оставшихся офсетом
            }
            

             

        }
    }
}
