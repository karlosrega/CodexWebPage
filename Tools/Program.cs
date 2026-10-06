using System;
using System.IO;
using System.Text;
using A1IntegrationsAdmin.Data;

namespace A1IntegrationsAdmin.Tools
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    Console.WriteLine("Commands: migrate <Database directory> | create-admin | check");
                    return 1;
                }
                var database = new Database();
                switch (args[0])
                {
                    case "migrate":
                        using (var connection = database.Open())
                        {
                            var files = Directory.GetFiles(args.Length > 1 ? args[1] : "Database", "*.sql");
                            Array.Sort(files, StringComparer.Ordinal);
                            foreach (var file in files)
                                using (var command = Database.Command(connection, null, File.ReadAllText(file)))
                                {
                                    command.ExecuteNonQuery();
                                    Console.WriteLine("Applied: " + Path.GetFileName(file));
                                }
                        }
                        break;
                    case "create-admin":
                        Console.Write("Usuario: ");
                        var user = Console.ReadLine();
                        Console.Write("Nombre: ");
                        var name = Console.ReadLine();
                        Console.Write("Correo: ");
                        var email = Console.ReadLine();
                        Console.Write("Contraseña (oculta): ");
                        var password = ReadSecret();
                        Console.Write("Confirmar contraseña: ");
                        var confirmation = ReadSecret();
                        if (password != confirmation)
                            throw new InvalidOperationException("Las contraseñas no coinciden.");
                        new AdminRepository(database).Bootstrap(user, name, email, password);
                        Console.WriteLine("Administrador inicial creado.");
                        break;
                    case "bootstrap-local":
                        var initialPassword = A1IntegrationsAdmin.Core.PasswordHasher.Token();
                        new AdminRepository(database).Bootstrap("admin", "Administrador", "admin@localhost.invalid", initialPassword);
                        Directory.CreateDirectory(".local");
                        File.WriteAllText(".local/initial-access.txt", "URL: http://localhost:5080/\r\nUsuario: admin\r\nContraseña: " + initialPassword + "\r\nActualiza tu nombre y correo, y cambia la contraseña desde Mi cuenta.\r\n");
                        Console.WriteLine("Administrador creado; acceso guardado únicamente en .local/initial-access.txt.");
                        break;
                    case "check":
                        using (var connection = database.Open())
                        using (var command = Database.Command(connection, null, "SELECT count(*) FROM a1admin.users"))
                            Console.WriteLine("Database OK; users: " + command.ExecuteScalar());
                        break;
                    default:
                        return 1;
                }
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Operation failed: " + exception.GetType().Name);
                if (exception is A1IntegrationsAdmin.Core.RuleException)
                    Console.Error.WriteLine(exception.Message);
                return 1;
            }
        }
        private static string ReadSecret()
        {
            var result = new StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return result.ToString();
                }
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (result.Length > 0)
                        result.Length--;
                }
                else if (!char.IsControl(key.KeyChar) && result.Length < 128)
                    result.Append(key.KeyChar);
            }
        }
    }
}
