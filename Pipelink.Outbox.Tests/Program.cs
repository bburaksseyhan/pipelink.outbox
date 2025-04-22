using Microsoft.EntityFrameworkCore;
using Pipelink.Outbox.Data;
using Pipelink.Outbox.Interfaces;
using Pipelink.Outbox.Models;
using Pipelink.Outbox.Pipeline;
using Pipelink.Outbox.Steps;

namespace Pipelink.Outbox.Tests;

public class Program
{
    private static WebApplication ConfigureApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Add DbContext
        builder.Services.AddDbContext<OutboxDbContext>(options =>
            options.UseInMemoryDatabase("OutboxDb"));

        // Register Outbox Pipeline Steps
        builder.Services.AddScoped<IPipelineStep<OutboxMessage>, SaveToOutboxStep>();
        // Add more steps here as needed

        // Register Outbox Pipeline
        builder.Services.AddScoped<IOutboxPipeline<OutboxMessage>, OutboxPipeline<OutboxMessage>>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

    public static void Main(string[] args)
    {
        var app = ConfigureApp(args);
        app.Run();
    }
} 