using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.Model;
using AmiyaBotPlayerRatingServer.Utility;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using static System.Net.Mime.MediaTypeNames;

namespace AmiyaBotPlayerRatingServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController(
    IConfiguration configuration,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOpenIddictApplicationManager oauthManager,
    PlayerRatingDatabaseContext dbContext)
    : ControllerBase
{
#pragma warning disable CS8618
    // ReSharper disable UnusedAutoPropertyAccessor.Global
    public class ChangeRoleRequest
    {
        public string UserId { get; set; }
        public string NewRole { get; set; }
        public string OldRole { get; set; }
    }
    public class RegisterModel
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string Nickname { get; set; }
        public string? ClaimedRole { get; set; }
    }
    public class LoginModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
    public class ChangePasswordModel
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
    public class ChangeUserInfoModel
    {
        public string? Nickname { get; set; }
        public string? Avatar { get; set; }
        public string? AvatarType { get; set; }
    }
    public class CreateClientModel
    {
        public string FriendlyName { get; set; }
        public string Description { get; set; }
        public string IconBase64 { get; set; }
        public string RedirectUri { get; set; }
    }
    // ReSharper restore UnusedAutoPropertyAccessor.Global
#pragma warning restore CS8618


    private static bool IsPasswordComplex(string password, int x)
    {
        string specialCharacters = "!@#$%^&*()-+";
        int count = 0;

        // 检查大写字母
        if (password.Any(char.IsUpper)) count++;
        // 检查小写字母
        if (password.Any(char.IsLower)) count++;
        // 检查数字
        if (password.Any(char.IsDigit)) count++;
        // 检查特殊字符
        if (password.Any(specialCharacters.Contains)) count++;

        return count >= x;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(); // 假设_context是你的数据库上下文

        if (!IsPasswordComplex(model.Password, 2))
        {
            return BadRequest(new { message = "密码不符合要求，至少需要包含大写字母、小写字母、数字和特殊符号（!@#$%^&*()-+）中的2种。" });
        }

        var user = new ApplicationUser { UserName = model.Email, Email = model.Email, Nickname = model.Nickname };
        var result = await userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            await transaction.RollbackAsync();
            return BadRequest(result.Errors);
        }

        var role = model.ClaimedRole == "开发者账户" ? "开发者账户" : "普通账户";
        var addToRoleResult = await userManager.AddToRoleAsync(user, role);

        if (!addToRoleResult.Succeeded)
        {
            await transaction.RollbackAsync();
            return BadRequest(new { message = "注册账户失败", errors = addToRoleResult.Errors });
        }

        await transaction.CommitAsync(); // 提交事务
        return Ok(new { message = "用户注册成功" });
    }

    [AllowAnonymous]
    [HttpPost("quickRegister")]
    public async Task<IActionResult> QuickRegister([FromBody] RegisterModel model)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(); // 假设_context是你的数据库上下文

        if (!String.IsNullOrWhiteSpace(model.Password))
        {
            if (!IsPasswordComplex(model.Password, 2))
            {
                return BadRequest(new { message = "密码不符合要求，至少需要包含大写字母、小写字母、数字和特殊符号（!@#$%^&*()-+）中的2种。" });
            }
        }
        else
        {
            // 生成一个随机复杂密码
            model.Password = CryptoHelper.GeneratePassword(16);
        }

        if (String.IsNullOrWhiteSpace(model.Email))
        {
            model.Email = Guid.NewGuid().ToString("N") + "@amiyabot.com";
        }

        var user = new ApplicationUser { UserName = model.Email, Email = model.Email, Nickname = model.Nickname };
        var result = await userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            await transaction.RollbackAsync();
            return BadRequest(result.Errors);
        }

        var role = model.ClaimedRole == "开发者账户" ? "开发者账户" : "普通账户";
        var addToRoleResult = await userManager.AddToRoleAsync(user, role);

        if (!addToRoleResult.Succeeded)
        {
            await transaction.RollbackAsync();
            return BadRequest(new { message = "注册账户失败", errors = addToRoleResult.Errors });
        }

        await transaction.CommitAsync(); // 提交事务

        // 然后立即登录
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
        };
        
        claims.Add(new Claim(ClaimTypes.Role, role));
        

        var key = Encoding.ASCII.GetBytes(configuration["JWT:Secret"]!);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(token);

        // 设置名为 "jwt" 的 Cookie
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddHours(24)
        };
        HttpContext.Response.Cookies.Append("jwt", jwtToken, cookieOptions);


        return Ok(new { Token = jwtToken, Email=user.Email });
    }
    
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var user = await userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return BadRequest(new { message = "邮箱或密码错误" });
        }

        var result = await userManager.CheckPasswordAsync(user, model.Password);
        if (!result)
        {
            return BadRequest(new { message = "邮箱或密码错误" });
        }

        var userRoles = await userManager.GetRolesAsync(user);  // 获取用户角色

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));  // 将角色添加为声明
        }

        var key = Encoding.ASCII.GetBytes(configuration["JWT:Secret"]!);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(token);

        // 设置名为 "jwt" 的 Cookie
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddHours(24)
        };
        HttpContext.Response.Cookies.Append("jwt", jwtToken, cookieOptions);


        return Ok(new { Token = jwtToken });
    }
    
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
    {
        // Get the current user ID from the claims
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Find the user
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        // Validate the current password
        var checkPassword = await userManager.CheckPasswordAsync(user, model.CurrentPassword);
        if (!checkPassword)
        {
            return BadRequest(new { message = "当前密码不正确" });
        }

        // Change the password
        var changePasswordResult = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

        if (!changePasswordResult.Succeeded)
        {
            return BadRequest(new { message = "密码更改失败", errors = changePasswordResult.Errors });
        }

        return Ok(new { message = "密码已成功更改" });
    }
    
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Authorize(Roles = "管理员账户")]
    [HttpPost("change-role")]
    public async Task<IActionResult> ChangeUserRole([FromBody] ChangeRoleRequest model)
    {
        var user = await userManager.FindByIdAsync(model.UserId);
        if (user == null)
        {
            return NotFound("用户不存在");
        }

        // 检查新角色是否存在
        if (!await roleManager.RoleExistsAsync(model.NewRole))
        {
            return BadRequest("指定的新角色不存在");
        }

        if (model.OldRole != null)
        {
            // 如果提供了旧角色，并且用户属于这个角色，则从旧角色中移除
            if (await userManager.IsInRoleAsync(user, model.OldRole))
            {
                await userManager.RemoveFromRoleAsync(user, model.OldRole);
            }
        }

        // 将用户添加到新角色
        var addToRoleResult = await userManager.AddToRoleAsync(user, model.NewRole);

        if (addToRoleResult.Succeeded)
        {
            return Ok($"用户角色已更改为 {model.NewRole}");
        }

        return BadRequest("无法更改用户角色");
    }
    
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("change-user-info")]
    public async Task<IActionResult> ChangeUserInfo([FromBody] ChangeUserInfoModel model)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrEmpty(model.Nickname))
        {
            user.Nickname = model.Nickname;
        }

        if (!string.IsNullOrEmpty(model.Avatar))
        {
            user.Avatar = model.Avatar;
        }

        if (!string.IsNullOrEmpty(model.AvatarType))
        {
            user.AvatarType = model.AvatarType;
        }

        await userManager.UpdateAsync(user);

        return Ok(new { message = "用户信息已更新" });
    }
    
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Authorize(Roles = "管理员账户,开发者账户")]
    [HttpPost("create-client")]
    public async Task<IActionResult> CreateClient([FromBody] CreateClientModel model)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = Guid.NewGuid().ToString("N"),
            ClientSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.Authorization,

                OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,

                OpenIddictConstants.Permissions.Prefixes.Scope + "TestReadData",

                OpenIddictConstants.Permissions.ResponseTypes.Code,
            },
            DisplayName = model.FriendlyName,
            RedirectUris = { new Uri(model.RedirectUri) }
        };

        await oauthManager.CreateAsync(descriptor);

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        dbContext.ClientInfos.Add(new ClientInfo
        {
            ClientId = descriptor.ClientId,
            FriendlyName = model.FriendlyName,
            Description = model.Description,
            IconBase64 = model.IconBase64,
            RedirectUri = model.RedirectUri,
            UserId = userId
        });

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            ClientId = descriptor.ClientId,
            ClientSecret = descriptor.ClientSecret,
            Scope = "TestReadData",
            IconBase64 = model.IconBase64
        });
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Authorize(Roles = "管理员账户,开发者账户,演示开发者账户")]
    [HttpGet("list-clients")]
    public async Task<IActionResult> ListClients()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var clients = await dbContext.ClientInfos.Where(c => c.UserId == userId).ToListAsync();
        return Ok(clients.Select(client=>new
        {
            ClientId = client.ClientId,
            FriendlyName = client.FriendlyName,
            Description = client.Description,
            Scope = "TestReadData",
            IconBase64 = client.IconBase64
        }));
    }

    [AllowAnonymous]
    [HttpGet("get-client/{clientId}")]
    public async Task<IActionResult> GetClient(string clientId)
    {
        var client = await dbContext.ClientInfos.FirstOrDefaultAsync(c => c.ClientId == clientId);
        if (client == null)
        {
            return NotFound();
        }

        var application = await oauthManager.FindByClientIdAsync(clientId);
        if (application == null)
        {
            return NotFound();
        }

        var redirectUris = await oauthManager.GetRedirectUrisAsync(application);

        var scopes =
            (await oauthManager.GetPermissionsAsync(application)).Where(p =>
                p.StartsWith(OpenIddictConstants.Permissions.Prefixes.Scope));

        return Ok(new
        {
            ClientId = client.ClientId,
            FriendlyName = client.FriendlyName,
            Description = client.Description,
            Scope = "TestReadData",
            IconBase64 = client.IconBase64,
            RedirectUri = redirectUris,
            Scopes = scopes
        });
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Authorize(Roles = "管理员账户,开发者账户")]
    [HttpDelete("delete-client/{clientId}")]
    public async Task<IActionResult> DeleteClient(string clientId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // 判断当前用户是否是管理员
        var isAdmin = User.IsInRole("管理员账户");

        ClientInfo? client;
        if (isAdmin)
        {
            // 管理员可以删除任何客户端
            client = await dbContext.ClientInfos.FirstOrDefaultAsync(c => c.ClientId == clientId);
        }
        else
        {
            // 开发者只能删除自己的客户端
            client = await dbContext.ClientInfos.FirstOrDefaultAsync(c => c.ClientId == clientId && c.UserId == userId);
        }


        if (client == null)
        {
            return NotFound("应用不存在或您没有权限删除这个客户端");
        }

        // 删除 OAuth2 客户端
        var openIddictApplication = await oauthManager.FindByClientIdAsync(clientId);
        if (openIddictApplication != null)
        {
            await oauthManager.DeleteAsync(openIddictApplication);
        }

        dbContext.ClientInfos.Remove(client);
        await dbContext.SaveChangesAsync();

        return Ok(new { message = "应用已成功删除" });
    }


    [HttpGet("describe")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Describe()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var userRoles = await userManager.GetRolesAsync(user);
        return Ok(new
        {
            Id = user.Id,
            Email = user.Email,
            Nickname = user.Nickname,
            Avatar = user.Avatar,
            AvatarType = user.AvatarType,
            Roles = userRoles
        });
    }

}