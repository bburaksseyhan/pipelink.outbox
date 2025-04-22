using OutboxPipelineExample.Models;
using Pipelink.Outbox.Interfaces;

namespace OutboxPipelineExample.Pipeline;

public class PaymentProcessingStep : IPipelineStep<OrderMessage>
{
    public async Task ExecuteAsync(OrderMessage message)
    {
        Console.WriteLine($"Processing payment for order {message.OrderId}...");
        
        // Simulate payment processing
        await Task.Delay(200); // Simulate network delay
        
        var paymentSuccessful = ProcessPayment(message.TotalAmount);
        if (!paymentSuccessful)
            throw new Exception("Payment processing failed");
            
        Console.WriteLine($"Payment processed successfully for order {message.OrderId}");
    }
    
    private bool ProcessPayment(decimal amount)
    {
        // Simulate payment processing
        return new Random().Next(0, 2) == 1; // 50% chance of success
    }
} 