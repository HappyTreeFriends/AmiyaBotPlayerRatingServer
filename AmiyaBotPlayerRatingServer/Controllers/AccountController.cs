using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using AmiyaBotPlayerRatingServer.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using static System.Net.Mime.MediaTypeNames;

namespace AmiyaBotPlayerRatingServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{

    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IOpenIddictApplicationManager _oauthManager;

    public AccountController(IConfiguration configuration,
        UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, 
        IOpenIddictApplicationManager oauthManager)
    {
        _configuration = configuration;
        _userManager = userManager;
        _roleManager = roleManager;
        _oauthManager = oauthManager;
    }

    public class RegisterModel
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
        var result = await _userManager.CreateAsync(user, model.Password);
        var addToRoleResult = await _userManager.AddToRoleAsync(user, "普通账户");

        if (result.Succeeded&& addToRoleResult.Succeeded)
        {
            return Ok(new { message = "用户注册成功" });
        }

        return BadRequest(result.Errors);
    }

    public class LoginModel
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return BadRequest(new { message = "用户名或密码错误" });
        }

        var result = await _userManager.CheckPasswordAsync(user, model.Password);
        if (!result)
        {
            return BadRequest(new { message = "用户名或密码错误" });
        }

        var userRoles = await _userManager.GetRolesAsync(user);  // 获取用户角色

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Id.ToString())
        };

        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));  // 将角色添加为声明
        }

        var key = Encoding.ASCII.GetBytes(_configuration["JWT:Secret"]!);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return Ok(new { Token = tokenHandler.WriteToken(token) });
    }

    // DTO (Data Transfer Object) 用于接收请求数据
    public class ChangeRoleRequest
    {
        public string? UserId { get; set; }
        public string? NewRole { get; set; }
        public string? OldRole { get; set; }
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Authorize(Roles = "管理员账户")]
    [HttpPost("change-role")]
    public async Task<IActionResult> ChangeUserRole([FromBody] ChangeRoleRequest model)
    {
        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user == null)
        {
            return NotFound("用户不存在");
        }

        // 检查新角色是否存在
        if (!await _roleManager.RoleExistsAsync(model.NewRole))
        {
            return BadRequest("指定的新角色不存在");
        }

        if (model.OldRole != null)
        {
            // 如果提供了旧角色，并且用户属于这个角色，则从旧角色中移除
            if (await _userManager.IsInRoleAsync(user, model.OldRole))
            {
                await _userManager.RemoveFromRoleAsync(user, model.OldRole);
            }
        }

        // 将用户添加到新角色
        var addToRoleResult = await _userManager.AddToRoleAsync(user, model.NewRole);

        if (addToRoleResult.Succeeded)
        {
            return Ok($"用户角色已更改为 {model.NewRole}");
        }

        return BadRequest("无法更改用户角色");
    }
    
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Authorize(Roles = "管理员账户,开发者账户")]
    [HttpPost("create-secret")]
    public async Task<IActionResult> CreateSecret()
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = Guid.NewGuid().ToString("N"),
            ClientSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                OpenIddictConstants.Permissions.Prefixes.Scope + "TestReadData"
            }
        };

        await _oauthManager.CreateAsync(descriptor);

        return Ok(new
        {
            ClientId = descriptor.ClientId,
            ClientSecret = descriptor.ClientSecret,
            Scope = "TestReadData"
        });
    }

}