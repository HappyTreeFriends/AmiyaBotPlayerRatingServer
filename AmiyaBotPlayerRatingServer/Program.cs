﻿using Aliyun.OSS;
using AmiyaBotPlayerRatingServer.Data;
using System.ComponentModel;
using System.Text;
using System.Text.Json.Serialization;
using AmiyaBotPlayerRatingServer.Controllers.Policy;
using AmiyaBotPlayerRatingServer.Utility;
using Hangfire;
using Hangfire.PostgreSql;
using DateTimeConverter = AmiyaBotPlayerRatingServer.Utility.DateTimeConverter;
using Microsoft.EntityFrameworkCore;
using AmiyaBotPlayerRatingServer.Model;
using AmiyaBotPlayerRatingServer.Utility.OpenIddict;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using OpenIddict.Validation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var env = builder.Environment;
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

var configuration = builder.Configuration;

builder.Services.AddControllers(config =>
{
    // 全局授权策略，所有未标记的接口都默认要求Authorize
    // 匿名接口需要显式使用[AllowAnonymous]标出
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    config.Filters.Add(new AuthorizeFilter(policy));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<PlayerRatingDatabaseContext>(options =>
{
    options.UseOpenIddict();
});

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
        x.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
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

builder.Services.AddOpenIddict()
    // 注册Entity Framework Core存储。
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<PlayerRatingDatabaseContext>();
    })
    // 注册AspNetCore组件。
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("/connect/authorize");
        options.SetTokenEndpointUris("/connect/token");
        options.SetUserinfoEndpointUris("/connect/userinfo");

        options.SetConfigurationEndpointUris("/connect/.well-known/openid-configuration");

        options.AllowClientCredentialsFlow();
        options.AllowAuthorizationCodeFlow();

        var securityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(configuration["JWT:Secret"]!));
        options.AddSigningKey(securityKey);
        options.AddEncryptionKey(securityKey);
        
        options.UseAspNetCore()
            .EnableTokenEndpointPassthrough().DisableTransportSecurityRequirement();
    }).AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddCredentialOwnerPolicy();
builder.Services.AddOpenIddictScopePolicy();

builder.Services.AddHttpClient();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

app.UseCors(c => c.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

app.UseAuthentication();
app.UseAuthorization();

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

app.UseHangfireDashboard("/api/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireCustomFilter() }
});

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
