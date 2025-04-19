using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public class AuthorizeCheckOperationFilter : IOperationFilter
{
  public void Apply(OpenApiOperation operation, OperationFilterContext context)
  {
    // Verifica si hay [Authorize] en la clase o método
    var hasAuthorize = context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() == true
                    || context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any();

    if (hasAuthorize)
    {
      operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
      operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });

      operation.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [ new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        }
                    ] = new List<string>()
                }
            ];
    }
  }
}
