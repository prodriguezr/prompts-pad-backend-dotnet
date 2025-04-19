using Novell.Directory.Ldap;
using Prodrigu.PromptsPad.Models.Ldap;

namespace Prodrigu.PromptsPad.Api.Services;

public class LdapAuthService : ILdapAuthService
{
    private readonly IConfiguration _config;

    public LdapAuthService(IConfiguration config)
    {
        _config = config;
    }

    public LdapUserInfo? ValidateCredentials(string username, string password)
    {
        try
        {
            var host = _config["Ldap:Domain"] ?? "localhost";
            var port = int.TryParse(_config["Ldap:Port"], out var p) ? p : 389;
            var baseDn = _config["Ldap:SearchBase"] ?? throw new InvalidOperationException("Missing Ldap:SearchBase");

            var userDn = $"cn={username},ou=users,{baseDn}";

            Console.WriteLine("📡 LdapAuthService.ValidateCredentials");
            Console.WriteLine($"🔸 host     = {host}");
            Console.WriteLine($"🔸 port     = {port}");
            Console.WriteLine($"🔸 baseDn   = {baseDn}");
            Console.WriteLine($"🔸 userDn   = {userDn}");
            Console.WriteLine($"🔸 username = {username}");
            Console.WriteLine($"🔸 password = {new string('*', password.Length)}");

            using var connection = new LdapConnection();
            connection.Connect(host, port);
            connection.Bind(userDn, password);

            // 🔍 Buscar datos del usuario
            var userSearch = connection.Search(
                baseDn,
                LdapConnection.ScopeSub,
                $"(cn={username})",
                new[] { "cn", "sn", "mail" },
                false
            );

            if (!userSearch.HasMore())
            {
                Console.WriteLine($"❌ Usuario '{username}' no encontrado en el árbol LDAP.");
                return null;
            }

            var entry = userSearch.Next();
            var cn = entry.GetAttribute("cn")?.StringValue ?? "";
            var sn = entry.GetAttribute("sn")?.StringValue ?? "";
            var mail = entry.GetAttribute("mail")?.StringValue ?? "";

            // 🔍 Verificar si pertenece al grupo "Aplicaciones"
            var groupSearch = connection.Search(
                baseDn,
                LdapConnection.ScopeSub,
                "(cn=Aplicaciones)",
                new[] { "member" },
                false
            );

            bool isInGroup = false;

            if (groupSearch.HasMore())
            {
                var groupEntry = groupSearch.Next();
                var members = groupEntry.GetAttribute("member")?.StringValueArray ?? [];

                isInGroup = members.Any(m => m.Equals(userDn, StringComparison.OrdinalIgnoreCase));
            }

            if (!isInGroup)
            {
                Console.WriteLine($"❌ El usuario '{username}' no pertenece al grupo 'Aplicaciones'");
                return null;
            }

            // ✅ Usuario válido y pertenece al grupo requerido
            return new LdapUserInfo
            {
                Username = username,
                Cn = cn,
                Sn = sn,
                Mail = mail,
                Groups = [] // no usamos memberOf en este caso
            };
        }
        catch (LdapException ex)
        {
            Console.WriteLine($"❌ Error LDAP: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error inesperado: {ex.Message}");
            return null;
        }
    }
}
