using Prodrigu.PromptsPad.Models.Ldap;

namespace Prodrigu.PromptsPad.Api.Services;

public interface ILdapAuthService
{
  LdapUserInfo? ValidateCredentials(string username, string password);
}
