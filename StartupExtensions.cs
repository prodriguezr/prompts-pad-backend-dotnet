using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.IdentityModel.Tokens;
using Prodrigu.PromptsPad.Api.Services;
using System.Text;

namespace Prodrigu.PromptsPad.Api;

public static class StartupExtensions
{
  public static void ConfigureAuthentication(this IServiceCollection services, IConfiguration config, IWebHostEnvironment env)
  {
    var mode = config["Authentication:Mode"];

    if (mode == "LDAP" && env.IsProduction())
    {
      // Autenticación con Windows/LDAP (solo en producción)
      services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
              .AddNegotiate();

      services.AddAuthorization();

      // Registramos el servicio incluso en macOS, pero con fallback
      services.AddScoped<ILdapAuthService, LdapAuthService>();
    }
    else
    {
      // Autenticación con JWT (por defecto)
      var jwt = config.GetSection("Jwt");
      var secret = jwt["Secret"];

      if (string.IsNullOrWhiteSpace(secret))
        throw new InvalidOperationException("JWT secret is not configured.");

      var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

      services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
              .AddJwtBearer(options =>
              {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                  ValidateIssuer = true,
                  ValidateAudience = true,
                  ValidateIssuerSigningKey = true,
                  ValidIssuer = jwt["Issuer"],
                  ValidAudience = jwt["Audience"],
                  IssuerSigningKey = key
                };
              });

      services.AddAuthorization();
    }
  }
}
