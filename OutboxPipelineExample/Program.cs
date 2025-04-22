using OutboxPipelineExample.Models;
using OutboxPipelineExample.Pipeline;
using Pipelink.Outbox.Interfaces;
using Pipelink.Outbox.Pipeline;

var order = new OrderMessage
{
    OrderId = Guid.NewGuid(),
    CustomerName = "John Doe",
    TotalAmount = 150.99m,
    CreatedAt = DateTime.UtcNow,
    Items = new List<OrderItem>
    {
        new OrderItem
        {
            ProductId = "P001",
            ProductName = "Laptop",
            Quantity = 1,
            UnitPrice = 999.99m
        },
        new OrderItem
        {
            ProductId = "P002",
            ProductName = "Mouse",
            Quantity = 2,
            UnitPrice = 25.50m
        }
    }
};

// Create pipeline steps
var steps = new List<IPipelineStep<OrderMessage>>
{
    new ValidationStep(),
    new InventoryCheckStep(),
    new PaymentProcessingStep(),
    new OrderFulfillmentStep()
};

// Create and configure the pipeline
var pipeline = new OutboxPipeline<OrderMessage>(steps);

try
{
    Console.WriteLine($"Starting order processing for order {order.OrderId}");
    await pipeline.ProcessAsync(order);
    Console.WriteLine("Order processing completed successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"Order processing failed: {ex.Message}");
}
