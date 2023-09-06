using Aliyun.OSS;
using AmiyaBotPlayerRatingServer.Data;
using System.ComponentModel;
using System.Text;
using System.Text.Json.Serialization;
using AmiyaBotPlayerRatingServer.Utility;
using Hangfire;
using Hangfire.PostgreSql;
using DateTimeConverter = AmiyaBotPlayerRatingServer.Utility.DateTimeConverter;
using Microsoft.EntityFrameworkCore;
using AmiyaBotPlayerRatingServer.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<PlayerRatingDatabaseContext>();
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<PlayerRatingDatabaseContext>()
    .AddDefaultTokenProviders();

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
builder.Services.AddSingleton<HangfireConfigurationService>();
builder.Services.AddHangfireServer();

builder.Services.AddAuthentication(x =>
    {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(x =>
    {
        x.RequireHttpsMetadata = false;
        x.SaveToken = true;
        x.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(configuration["JWT:Secret"]!)),
            ValidateIssuer = false,
            ValidateAudience = false,
        };
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//初始化一些Service
using (var scope = app.Services.CreateScope())
{
    //触发一次HangfireConfigurationService来初始化他
    _ = scope.ServiceProvider.GetRequiredService<HangfireConfigurationService>();

    //执行数据迁移
    var dbContext = scope.ServiceProvider.GetRequiredService<PlayerRatingDatabaseContext>();
    dbContext.Database.Migrate();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    if (!await roleManager.RoleExistsAsync("管理员账户"))
    {
        await roleManager.CreateAsync(new IdentityRole("管理员账户"));
    }

    if (!await roleManager.RoleExistsAsync("开发者账户"))
    {
        await roleManager.CreateAsync(new IdentityRole("开发者账户"));
    }

    if (!await roleManager.RoleExistsAsync("普通账户"))
    {
        await roleManager.CreateAsync(new IdentityRole("普通账户"));
    }
}

app.UseCors(c => c.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

app.UseAuthorization();
app.UseAuthentication();
app.MapControllers();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new MyAuthorizationFilter() }
});

app.Run();
