namespace Prodrigu.PromptsPad.Models.Ldap;

public class LdapUserInfo
{
  public string Username { get; set; } = string.Empty;
  public string Cn { get; set; } = string.Empty;
  public string Sn { get; set; } = string.Empty;
  public string Mail { get; set; } = string.Empty;
  public string[] Groups { get; set; } = [];
}
