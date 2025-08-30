using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using VideoUploader.Data.Database;
using VideoUploader.Data.Repositories;
using VideoUploader.Models.Models;
using VideoUploader.Services.MessageBus;
using VideoUploader.Services.Persistence;

var builder = WebApplication.CreateBuilder(args);

#region Health Checks

var rabbitMqConnectionString = $"amqp://{builder.Configuration["RabbitMQConnection:Username"]}:{builder.Configuration["RabbitMQConnection:Password"]}@{builder.Configuration["RabbitMQConnection:Host"]}";

builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        name: "Database",
        tags: ["core", "database"])
    .AddRabbitMQ(
        rabbitConnectionString: rabbitMqConnectionString,
        name: "RabbitMQ",
        tags: ["core", "message-bus"]);

#endregion

#region Dependency Injection

// Add services to the container.

// Context
builder.Services.AddDbContext<VideoUploaderContext>();

// Configuration
builder.Services.Configure<FileStorageSettings>(builder.Configuration.GetSection("FileStorageSettings"));

// Services
builder.Services.AddTransient<IVideoAnalysisService, VideoAnalysisService>();

// Repository
builder.Services.AddTransient<IVideoAnalysisRepository, VideoAnalysisRepository>();

// Message Bus
builder.Services.AddScoped<IMessageBus, MessageBus>();
builder.Services.AddScoped<IUploadVideoAnalysisProducer, UploadVideoAnalysisProducer>();

#endregion

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();

app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
