using OutboxPipelineExample.Models;
using Pipelink.Outbox.Interfaces;

namespace OutboxPipelineExample.Pipeline;

public class InventoryCheckStep : IPipelineStep<OrderMessage>
{
    public async Task ExecuteAsync(OrderMessage message)
    {
        Console.WriteLine($"Checking inventory for order {message.OrderId}...");
        
        // Simulate inventory check
        foreach (var item in message.Items)
        {
            // In a real application, this would check a database or external service
            await Task.Delay(100); // Simulate network delay
            
            var inStock = CheckInventory(item.ProductId, item.Quantity);
            if (!inStock)
                throw new Exception($"Product {item.ProductName} is out of stock");
                
            Console.WriteLine($"Product {item.ProductName} is in stock");
        }
        
        Console.WriteLine($"Inventory check completed for order {message.OrderId}");
    }
    
    private bool CheckInventory(string productId, int quantity)
    {
        // Simulate inventory check
        return new Random().Next(0, 2) == 1; // 50% chance of being in stock
    }
} 