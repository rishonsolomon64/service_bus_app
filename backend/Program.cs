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

// Service Bus configuration (edit with your values)
const string ServiceBusConnection =
    "Endpoint=sb://rishontryingplswork.servicebus.windows.net/;SharedAccessKeyName=try1app1;SharedAccessKey=ueUUphS6h9qNaN4tQP9CQVEO2fyEaovgn+ASbJ4/AG8=";

const string PublishTopic = "servicetest";
const string ScheduleTopic = "servicetest";

//----------------------------------------------------------------------

// Reusable singleton client
builder.Services.AddSingleton(new ServiceBusClient(ServiceBusConnection));

// PublishTask 
builder.Services.AddScoped<PublishTask>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<PublishTask>>();
    var logContext = sp.GetRequiredService<ServiceBusLogContext>();
    return new PublishTask(
        ServiceBusConnection,
        PublishTopic,
        logger,
        logContext
    );
});


// ScheduleTask 
builder.Services.AddSingleton<ScheduleTask>(sp =>
{
    var client       = sp.GetRequiredService<ServiceBusClient>();
    var logger       = sp.GetRequiredService<ILogger<ScheduleTask>>();
    var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();  
    return new ScheduleTask(
        client,
        ScheduleTopic, 
        logger,
        scopeFactory     
    );
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy => policy.WithOrigins("http://localhost:4200")
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

app.UseHttpsRedirection();
app.UseCors("AllowAngularApp");
app.UseAuthorization();
app.MapControllers();
app.Run();

