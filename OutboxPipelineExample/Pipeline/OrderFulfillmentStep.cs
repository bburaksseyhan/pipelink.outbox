using OutboxPipelineExample.Models;
using Pipelink.Outbox.Interfaces;

namespace OutboxPipelineExample.Pipeline;

public class OrderFulfillmentStep : IPipelineStep<OrderMessage>
{
    public async Task ExecuteAsync(OrderMessage message)
    {
        Console.WriteLine($"Fulfilling order {message.OrderId}...");
        
        // Simulate order fulfillment
        await Task.Delay(300); // Simulate processing time
        
        Console.WriteLine($"Order {message.OrderId} has been fulfilled");
        Console.WriteLine($"Shipping to: {message.CustomerName}");
        Console.WriteLine($"Total items: {message.Items.Sum(i => i.Quantity)}");
        Console.WriteLine($"Total amount: {message.TotalAmount:C}");
    }
} 