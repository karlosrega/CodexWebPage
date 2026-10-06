using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using A1IntegrationsAdmin.Core;
using A1IntegrationsAdmin.Data;
using A1IntegrationsAdmin.Web.Controllers;
using A1IntegrationsAdmin.Web.Infrastructure;

namespace A1IntegrationsAdmin.Tests
{
    /// <summary>Executable assertions plus real integration tests in a dedicated *_tests database.</summary>
    internal static class Program
    {
        private static int passed;
        private const string Password = "Test-only-password-2026!";
        private static void Assert(bool condition, string name)
        {
            if (!condition)
                throw new Exception(name);
            passed++;
            Console.WriteLine("PASS " + name);
        }
        private static void Reject(Action operation, string name)
        {
            bool rejected = false;
            try
            {
                operation();
            }
            catch (RuleException) { rejected = true; }
            Assert(rejected, name);
        }

        private static int Main(string[] args)
        {
            try
            {
                var hash = PasswordHasher.Hash(Password);
                Assert(PasswordHasher.Verify(Password, hash), "PBKDF2 correct password");
                Assert(!PasswordHasher.Verify("Wrong-password-2026!", hash), "PBKDF2 wrong password");
                Assert(hash != PasswordHasher.Hash(Password), "Independent random salts");
                Assert(!PasswordHasher.Verify(Password, "not-a-hash"), "Malformed hash rejected");
                Assert(!PasswordHasher.Verify(Password, "pbkdf2-sha256$1$AA==$AA=="), "Weak or invalid hash rejected");
                Reject(() => PasswordHasher.Hash("short"), "Short password rejected");
                Reject(() => PasswordHasher.Hash(new string('x', 129)), "Oversized password rejected");
                Assert(PasswordHasher.Token() != PasswordHasher.Token(), "Random sessions");
                Assert(PasswordHasher.TokenHash("token") != "token", "Only session token hash stored");
                Reject(() => AdminRepository.ValidateUser(new UserRecord { UserName = "bad user", DisplayName = "Name", Email = "valid@example.invalid" }), "Invalid username rejected");
                Reject(() => AdminRepository.ValidateUser(new UserRecord { UserName = "valid", DisplayName = "Name", Email = "invalid" }), "Invalid email rejected");
                var controllers = new[] { typeof(PublicController), typeof(UsersController), typeof(ProfilesController), typeof(AccountController) };
                foreach (var type in controllers)
                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                        if (method.IsDefined(typeof(HttpPostAttribute), true))
                            Assert(method.IsDefined(typeof(ValidateAntiForgeryTokenAttribute), true), type.Name + "." + method.Name + " CSRF protected");
                foreach (var type in new[] { typeof(UsersController), typeof(ProfilesController) })
                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                        Assert(method.IsDefined(typeof(PermissionAttribute), true), type.Name + "." + method.Name + " permission protected");

                string connection = Environment.GetEnvironmentVariable("A1ADMIN_TEST_CONNECTION_STRING");
                if (string.IsNullOrEmpty(connection))
                {
                    Console.WriteLine("ALERT Integration tests require A1ADMIN_TEST_CONNECTION_STRING ending in _tests.");
                    return 2;
                }
                var builder = new Npgsql.NpgsqlConnectionStringBuilder(connection);
                if (!builder.Database.EndsWith("_tests", StringComparison.Ordinal))
                    throw new Exception("Refusing integration tests outside dedicated *_tests database.");
                var database = new Database(connection);
                using (var db = database.Open())
                {
                    foreach (var file in Directory.GetFiles(args.Length > 0 ? args[0] : "Database", "*.sql").OrderBy(p => p))
                        using (var command = Database.Command(db, null, File.ReadAllText(file)))
                            command.ExecuteNonQuery();
                    using (var command = Database.Command(db, null, "TRUNCATE a1admin.audit_events,a1admin.sessions,a1admin.user_profiles,a1admin.profile_permissions,a1admin.users RESTART IDENTITY; INSERT INTO a1admin.profile_permissions SELECT p.id,q.code FROM a1admin.profiles p CROSS JOIN a1admin.permissions q WHERE p.is_system"))
                        command.ExecuteNonQuery();
                }
                var admin = new AdminRepository(database);
                var auth = new AuthService(database);
                var id = admin.Bootstrap("admin", "Administrator", "admin@example.invalid", Password);
                Reject(() => admin.Bootstrap("another", "Another", "another@example.invalid", Password), "Bootstrap disabled after first account");
                var token = auth.Login("ADMIN", Password);
                Assert(token != null, "Case-insensitive login");
                Assert(auth.Resolve(token).Permissions.Count == 4, "Administrator permissions loaded");
                Assert(auth.Resolve("invalid-token") == null, "Invalid session rejected");
                Assert(auth.Login("unknown", Password) == null, "Unknown user rejected");
                Assert(admin.Promotions().Count == 5, "Five database-backed promotions");
                var own = admin.User(id);
                own.Active = false;
                Reject(() => admin.SaveUser(own, null, id), "Last administrator deactivation blocked");
                Assert(admin.User(id).Active, "Rejected write rolled back");
                own = admin.User(id);
                own.ProfileIds = new int[0];
                Reject(() => admin.SaveUser(own, null, id), "Last administrator profile removal blocked");
                var system = admin.Profiles().Single(p => p.System);
                Reject(() => admin.SaveProfile(system, id), "Administrator profile immutable");
                var profile = new ProfileRecord { Name = "Read-only", Description = "Testing", Active = true, Permissions = new[] { "users.read" } };
                // Repeated test runs retain named profiles; use the existing one when present.
                profile.Id = admin.Profiles().FirstOrDefault(p => p.Name == profile.Name)?.Id ?? 0;
                admin.SaveProfile(profile, id);
                var user = new UserRecord { UserName = "reader", DisplayName = "Reader", Email = "reader@example.invalid", Active = true, ProfileIds = new[] { profile.Id } };
                var userId = admin.SaveUser(user, Password, id);
                var readerToken = auth.Login("reader", Password);
                Assert(auth.Resolve(readerToken).Can("users.read") && !auth.Resolve(readerToken).Can("users.write"), "Read-only permissions respected");
                Reject(() => admin.SaveUser(admin.User(userId), null, userId), "Repository rejects unauthorized write");
                Assert(admin.Users("reader", 1).Total == 1, "Search and pagination");
                Assert(admin.Users("' OR 1=1 --", 1).Total == 0, "SQL search injection neutralized");
                Reject(() => admin.SaveUser(new UserRecord { UserName = "READER", DisplayName = "Duplicate", Email = "duplicate@example.invalid", Active = true }, Password, id), "Duplicate username rejected");
                profile.Permissions = new[] { "users.read", "profiles.read" };
                admin.SaveProfile(profile, id);
                Assert(auth.Resolve(readerToken) == null, "Profile changes revoke existing sessions");
                readerToken = auth.Login("reader", Password);
                Assert(auth.Resolve(readerToken).Can("profiles.read"), "Changed permission available after login");
                user = admin.User(userId);
                user.Active = false;
                admin.SaveUser(user, null, id);
                Assert(auth.Resolve(readerToken) == null && auth.Login("reader", Password) == null, "Deactivated account loses session and login");
                user.Active = true;
                admin.SaveUser(user, null, id);
                for (int i = 0; i < 5; i++)
                    Assert(auth.Login("reader", "incorrect-password") == null, "Failed login " + (i + 1));
                Assert(auth.Login("reader", Password) == null, "Lockout after five failures");
                using (var db = database.Open())
                using (var command = Database.Command(db, null, "UPDATE a1admin.users SET locked_until=now()-interval '1 minute' WHERE id=@id", "id", userId))
                    command.ExecuteNonQuery();
                Assert(auth.Login("reader", "incorrect-password") == null, "First failure after lockout expiry");
                readerToken = auth.Login("reader", Password);
                Assert(readerToken != null, "Expired lockout resets attempt counter");
                auth.Logout(readerToken);
                Assert(auth.Resolve(readerToken) == null, "Logout revokes server session");
                readerToken = auth.Login("reader", Password);
                using (var db = database.Open())
                using (var command = Database.Command(db, null, "UPDATE a1admin.sessions SET expires_at=now()-interval '1 minute' WHERE token_hash=@hash", "hash", PasswordHasher.TokenHash(readerToken)))
                    command.ExecuteNonQuery();
                Assert(auth.Resolve(readerToken) == null, "Expired session rejected");
                readerToken = auth.Login("reader", Password);
                Reject(() => auth.ChangePassword(userId, "wrong-password", "New-test-password-2026!"), "Wrong current password rejected");
                auth.ChangePassword(userId, Password, "New-test-password-2026!");
                Assert(auth.Resolve(readerToken) == null && auth.Login("reader", Password) == null, "Password change revokes sessions and old credential");
                Assert(auth.Login("reader", "New-test-password-2026!") != null, "New password accepted");
                using (var db = database.Open())
                using (var command = Database.Command(db, null, "SELECT count(*) FROM a1admin.audit_events"))
                    Assert(Convert.ToInt32(command.ExecuteScalar()) >= 10, "Audit records written");
                Console.WriteLine("RESULT " + passed + " checks passed; 0 failures.");
                return 0;
            }
            catch (Exception exception) { Console.Error.WriteLine("FAIL " + exception.GetType().Name + ": " + exception.Message); return 1; }
        }
    }
}
