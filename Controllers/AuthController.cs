using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Prodrigu.PromptsPad.Api.Services;
using Prodrigu.PromptsPad.Models.Auth;
using Prodrigu.PromptsPad.Models.Ldap;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Prodrigu.PromptsPad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
  private readonly IConfiguration _config;
  private readonly ILdapAuthService _ldapAuthService;

  public AuthController(IConfiguration config, ILdapAuthService ldapAuthService)
  {
    _config = config;
    _ldapAuthService = ldapAuthService;
  }

  [HttpPost("login")]
  public IActionResult Login([FromBody] LoginRequest req)
  {
    var ldapUser = _ldapAuthService.ValidateCredentials(req.Username, req.Password);

    if (ldapUser is null)
      return Unauthorized("Credenciales inválidas (LDAP)");

    var token = GenerateJwt(ldapUser);

    return Ok(new
    {
      access_token = token,
      token_type = "Bearer",
      expires_at = DateTime.UtcNow.AddHours(1),
      user = ldapUser.Username
    });
  }

  [Authorize]
  [HttpGet("profile")]
  public IActionResult GetProfile()
  {
    var identity = HttpContext.User.Identity as ClaimsIdentity;
    if (identity == null) return Unauthorized();

    var claims = identity.Claims.ToDictionary(c => c.Type, c => c.Value);

    return Ok(new
    {
      username = claims["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"],
      email = claims.GetValueOrDefault("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"),
      givenName = claims.GetValueOrDefault("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname"),
      surname = claims.GetValueOrDefault("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname"),
      role = claims.GetValueOrDefault("http://schemas.microsoft.com/ws/2008/06/identity/claims/role"),
    });
  }



  private string GenerateJwt(LdapUserInfo user)
  {
    var jwtSection = _config.GetSection("Jwt");
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Secret"]!));

    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Mail ?? ""),
            new Claim(ClaimTypes.GivenName, user.Cn ?? ""),
            new Claim(ClaimTypes.Surname, user.Sn ?? ""),
            new Claim(ClaimTypes.Role, "user")
        };

    // Agregar todos los grupos como claims individuales
    foreach (var group in user.Groups)
    {
      claims.Add(new Claim("group", group));
    }

    var token = new JwtSecurityToken(
        issuer: jwtSection["Issuer"],
        audience: jwtSection["Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: creds
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
  }
}
