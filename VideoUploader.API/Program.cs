using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using VideoUploader.API.Hubs;
using VideoUploader.API.Services;
using VideoUploader.Data.Database;
using VideoUploader.Data.Repositories;
using VideoUploader.Models.Configurations;
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

// Add services to the container

// Context
builder.Services.AddDbContext<VideoUploaderContext>();

// Configuration
builder.Services.Configure<FileStorageSettings>(builder.Configuration.GetSection("FileStorageSettings"));
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));

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

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();

app.UseSwaggerUI();

app.UseDefaultFiles();

app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHub<NotificationHub>("/notificationHub");

app.Run();