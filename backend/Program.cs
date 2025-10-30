// importing services
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using servicebusapi2.Publish;
using servicebusapi2.Schedule;
using servicebusapi2.Services;

var builder = WebApplication.CreateBuilder(args);

// logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

// EF Core DbContext
builder.Services.AddDbContext<ServiceBusLogContext>(options =>
    options.UseSqlite("Data Source=servicebus_logs.db"));

// Controllers
builder.Services.AddControllers();

// Supporting services 
builder.Services.AddSingleton<MessageIntervalService>();
//----------------------------------------------------------------------

// Service Bus configuration is now loaded from environment variables
var serviceBusConnection = builder.Configuration["ServiceBusConnection"]
    ?? throw new InvalidOperationException("Service Bus Connection string not found in configuration.");
var publishTopic = builder.Configuration["PublishTopic"]
    ?? throw new InvalidOperationException("PublishTopic not found in configuration.");
var scheduleTopic = builder.Configuration["ScheduleTopic"]
    ?? throw new InvalidOperationException("ScheduleTopic not found in configuration.");
//----------------------------------------------------------------------

// Reusable singleton client
builder.Services.AddSingleton(new ServiceBusClient(serviceBusConnection));

// PublishTask 
builder.Services.AddScoped<PublishTask>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<PublishTask>>();
    var logContext = sp.GetRequiredService<ServiceBusLogContext>();
    return new PublishTask(
        serviceBusConnection,
        publishTopic,
        logger,
        logContext
    );
});


// ScheduleTask 
builder.Services.AddSingleton<ScheduleTask>(sp =>
{
    var client = sp.GetRequiredService<ServiceBusClient>();
    var logger = sp.GetRequiredService<ILogger<ScheduleTask>>();
    var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
    return new ScheduleTask(
        client,
        scheduleTopic,
        logger,
        scopeFactory
    );
});

// CORS
var angularAppUrl = builder.Configuration["AngularAppUrl"] ?? "http://localhost:4200";
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy => policy.WithOrigins(angularAppUrl)
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Apply migrations (creates DB/table if missing)
using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<ServiceBusLogContext>();
    ctx.Database.Migrate();
}

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Exception handling middleware to log errors
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An unhandled exception has occurred.");
        throw; // Re-throw the exception
    }
});

// app.UseHttpsRedirection(); // Commented out for now to simplify proxy configuration
app.UseCors("AllowAngularApp");
app.UseAuthorization();
app.MapControllers();
app.Run();
