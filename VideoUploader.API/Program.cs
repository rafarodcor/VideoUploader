using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using VideoUploader.API.Hubs;
using VideoUploader.API.Services;
using VideoUploader.Data.Database;
using VideoUploader.Data.Repositories;
using VideoUploader.Models.Configurations;
using VideoUploader.Services.MessageBus;
using VideoUploader.Services.Persistence;

#region Builder

var builder = WebApplication.CreateBuilder(args);

#region Health Checks

var rabbitMqConnectionString = $"amqp://{builder.Configuration["RabbitMQConnection:Username"]}:{builder.Configuration["RabbitMQConnection:Password"]}@{builder.Configuration["RabbitMQConnection:Host"]}";

builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        name: "Database (SqlServer)",
        tags: ["core", "database"])
    .AddRabbitMQ(
        rabbitConnectionString: rabbitMqConnectionString,
        name: "RabbitMQ",
        tags: ["core", "message-bus"])
    .AddRedis(
        redisConnectionString: builder.Configuration.GetConnectionString("RedisConnection"),
        name: "Redis",
        tags: ["core", "cache", "backplane"])
    .AddMongoDb(
        mongodbConnectionString: builder.Configuration["MongoDbSettings:ConnectionString"],
        name: "Database (MongoDB)",
        tags: ["core", "database", "nosql"]);

#endregion

#region Dependency Injection

// Add services to the container

// Context
builder.Services.AddDbContext<VideoUploaderContext>();

// Configuration
builder.Services.Configure<FileStorageSettings>(builder.Configuration.GetSection("FileStorageSettings"));
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.Configure<UploadSettings>(builder.Configuration.GetSection("UploadSettings"));

// Services
builder.Services.AddTransient<IVideoAnalysisService, VideoAnalysisService>();
builder.Services.AddTransient<IVideoAnalysisMongoService, VideoAnalysisMongoService>();

// Repository
builder.Services.AddTransient<IVideoAnalysisRepository, VideoAnalysisRepository>();
builder.Services.AddTransient<IVideoAnalysisMongoRepository, VideoAnalysisMongoRepository>();

// Message Bus
builder.Services.AddScoped<IMessageBus, MessageBus>();
builder.Services.AddScoped<IUploadVideoAnalysisProducer, UploadVideoAnalysisProducer>();
builder.Services.AddHostedService<RedisNotificationListener>();

#endregion

#region SignalR

var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection");
builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisConnectionString, options =>
    {
        options.Configuration.ChannelPrefix = "VideoUploader";
    });

#endregion

#region Rate Limiter

builder.Services.AddRateLimiter(options =>
{
    // Define uma pol�tica de limita��o chamada "fixed"
    options.AddFixedWindowLimiter(policyName: "fixed", opt =>
    {
        opt.PermitLimit = 5; // M�ximo de 5 requisi��es
        opt.Window = TimeSpan.FromMinutes(1); // Dentro de uma janela de 1 minuto
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2; // Coloca no m�ximo 2 requisi��es em fila se o limite for atingido
    });
});

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

#region Database Migration

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Iniciando a migra��o do banco de dados...");
        var context = services.GetRequiredService<VideoUploaderContext>();
        await context.Database.MigrateAsync();
        logger.LogInformation("Migra��o do banco de dados conclu�da com sucesso.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Ocorreu um erro durante a migra��o do banco de dados.");
    }
}

#endregion

// Configure the HTTP request pipeline.
app.UseSwagger();

app.UseSwaggerUI();

app.UseDefaultFiles();

app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHub<NotificationHub>("/notificationHub");

app.Run();

#endregion