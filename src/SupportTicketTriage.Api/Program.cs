using Microsoft.EntityFrameworkCore;
using SupportTicketTriage.Api.Correlation;
using SupportTicketTriage.Api.Errors;
using SupportTicketTriage.Application.Abstractions;
using SupportTicketTriage.Infrastructure;
using SupportTicketTriage.Infrastructure.Persistence;
using SupportTicketTriage.Infrastructure.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
    });

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Support Ticket Intake & AI Triage API",
        Version = "v1",
        Description = "Receives support tickets, classifies them with a deterministic mock, persists to SQLite, and publishes TicketCreated to Azure Service Bus when configured."
    });
});

builder.Services.AddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Demo"))
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Support Ticket Triage v1");
    });
}

app.UseHttpsRedirection();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();

public partial class Program;
