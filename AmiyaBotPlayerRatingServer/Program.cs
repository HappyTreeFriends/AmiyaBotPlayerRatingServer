using Aliyun.OSS;
using AmiyaBotPlayerRatingServer.Data;
using System.ComponentModel;
using System.Text.Json.Serialization;
using AmiyaBotPlayerRatingServer.Utility;
using Hangfire;
using Hangfire.PostgreSql;
using DateTimeConverter = AmiyaBotPlayerRatingServer.Utility.DateTimeConverter;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build();

var serviceProvider = new ServiceCollection()
    .AddLogging(builder =>
    {
        builder.AddConsole(); // 添加控制台日志输出
    })
    .BuildServiceProvider();

var logger = serviceProvider.GetService<ILogger<object>>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<PlayerRatingDatabaseContext>();
builder.Services.AddScoped(_ => new OssClient(configuration["Aliyun:Oss:EndPoint"],
    configuration["Aliyun:Oss:Key"],
    configuration["Aliyun:Oss:Secret"]));
builder.Services.AddControllers()
    .AddMvcOptions(o => o.ReturnHttpNotAcceptable = false)
    .AddJsonOptions(o =>
    {
            o.JsonSerializerOptions.Converters.Add(new DateTimeConverter());
            o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

builder.Services.AddHangfire(hfConf => hfConf
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings());

// Add the processing server as IHostedService
builder.Services.AddHangfireServer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

GlobalConfiguration.Configuration.UsePostgreSqlStorage(PlayerRatingDatabaseContext.GetConnectionString(configuration));

//执行数据库迁移
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PlayerRatingDatabaseContext>();
    dbContext.Database.Migrate();
}

app.UseCors(c => c.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

app.UseAuthorization();

app.MapControllers();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new MyAuthorizationFilter() }
});

app.Run();
