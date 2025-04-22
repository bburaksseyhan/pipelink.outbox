using OutboxPipelineExample.Models;
using Pipelink.Outbox.Interfaces;

namespace OutboxPipelineExample.Pipeline;

public class ValidationStep : IPipelineStep<OrderMessage>
{
    public async Task ExecuteAsync(OrderMessage message)
    {
        Console.WriteLine($"Validating order {message.OrderId}...");
        
        if (string.IsNullOrEmpty(message.CustomerName))
            throw new ArgumentException("Customer name is required");
            
        if (message.TotalAmount <= 0)
            throw new ArgumentException("Total amount must be greater than zero");
            
        if (message.Items == null || !message.Items.Any())
            throw new ArgumentException("Order must contain at least one item");
            
        foreach (var item in message.Items)
        {
            if (string.IsNullOrEmpty(item.ProductId))
                throw new ArgumentException("Product ID is required");
                
            if (item.Quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero");
        }
        
        Console.WriteLine($"Order {message.OrderId} validation successful");
    }
} 