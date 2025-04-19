using Microsoft.AspNetCore.Routing;
using System.Text.RegularExpressions;

namespace PromptsPad.Infraestructure.Routing
{
  public class KebabCaseParameterTransformer : IOutboundParameterTransformer
  {
    public string? TransformOutbound(object? value)
    {
      if (value == null) return null;

      // Convierte "MyControllerName" a "my-controller-name"
      return Regex.Replace(value.ToString()!, "([a-z])([A-Z])", "$1-$2").ToLower();
    }
  }
}
