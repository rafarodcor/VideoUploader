using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using MongoDB.Driver;
using VideoUploader.Consumer.MessageBus;
using VideoUploader.Consumer.Services;
using VideoUploader.Data.Database;
using VideoUploader.Data.Repositories;
using VideoUploader.Models.Configurations;

#region Builder

var builder = WebApplication.CreateBuilder(args);

#region Health Checks

var rabbitMqConnectionString = $"amqp://{builder.Configuration["RabbitMQConnection:Username"]}:{builder.Configuration["RabbitMQConnection:Password"]}@{builder.Configuration["RabbitMQConnection:Host"]}";

builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        name: "Database (SqlServer)",
        tags: ["core", "database", "sql"])
    .AddRabbitMQ(
        rabbitConnectionString: rabbitMqConnectionString,
        name: "RabbitMQ",
        tags: ["core", "message-bus"])
    .AddRedis(
        redisConnectionString: builder.Configuration.GetConnectionString("RedisConnection"),
        name: "Redis",
        tags: ["core", "cache", "backplane"])
    .AddMongoDb(
        clientFactory: sp => sp.GetRequiredService<IMongoClient>(),
        name: "Database (MongoDB)",
        tags: ["core", "database", "nosql"]);

#endregion

#region Dependency Injection

// Add services to the container.

// Context
builder.Services.AddDbContext<VideoUploaderContext>();
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(builder.Configuration["MongoDbSettings:ConnectionString"]));

// Configuration
builder.Services.Configure<FileStorageSettings>(builder.Configuration.GetSection("FileStorageSettings"));
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));

// Services
builder.Services.AddTransient<IQrCodeVideoAnalysis, QrCodeVideoAnalysis>();
builder.Services.AddSingleton<INotificationService, RedisNotificationService>();

// Repository
builder.Services.AddTransient<IVideoAnalysisRepository, VideoAnalysisRepository>();
builder.Services.AddTransient<IVideoAnalysisMongoRepository, VideoAnalysisMongoRepository>();

// Message Bus
builder.Services.AddSingleton<RabbitMQConnectionManager>();
builder.Services.AddHostedService<UploadVideoAnalysisConsumer>();

#endregion

#region Swagger

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

#endregion

builder.Services.AddControllers();

#endregion

#region App

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

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();

#endregion