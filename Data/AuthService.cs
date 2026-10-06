using System;
using A1IntegrationsAdmin.Core;

namespace A1IntegrationsAdmin.Data
{
    /// <summary>Database-backed sessions and transactional lockout prevent parallel-login races.</summary>
    public sealed class AuthService
    {
        private readonly Database database;
        // A fixed dummy hash makes an unknown user perform the same derivation as a known user.
        private const string Dummy = "pbkdf2-sha256$600000$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
        public AuthService(Database database)
        {
            this.database = database;
        }

        public string Login(string username, string password)
        {
            using (var connection = database.Open())
            using (var transaction = connection.BeginTransaction())
            {
                int id = 0, failures = 0;
                string hash = Dummy;
                bool eligible = false;
                using (var command = Database.Command(connection, transaction,
                    "SELECT id,password_hash,active,CASE WHEN locked_until<=now() THEN 0 ELSE failed_attempts END,(locked_until IS NULL OR locked_until<=now()) FROM a1admin.users WHERE lower(username)=lower(@username) FOR UPDATE",
                    "username", (username ?? "").Trim()))
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        id = reader.GetInt32(0);
                        hash = reader.GetString(1);
                        failures = reader.GetInt32(3);
                        eligible = reader.GetBoolean(2) && reader.GetBoolean(4);
                    }
                }
                var verified = PasswordHasher.Verify(password, hash);
                if (!verified || !eligible)
                {
                    if (id != 0 && eligible)
                    {
                        using (var command = Database.Command(connection, transaction,
                            "UPDATE a1admin.users SET failed_attempts=@attempts,locked_until=CASE WHEN @attempts>=5 THEN now()+interval '15 minutes' ELSE NULL END WHERE id=@id",
                            "attempts", failures + 1, "id", id))
                            command.ExecuteNonQuery();
                    }
                    Database.Audit(connection, transaction, id, "login.failed", id == 0 ? (int?)null : id);
                    transaction.Commit();
                    return null;
                }
                string token = PasswordHasher.Token();
                using (var command = Database.Command(connection, transaction,
                    "UPDATE a1admin.users SET failed_attempts=0,locked_until=NULL WHERE id=@id; DELETE FROM a1admin.sessions WHERE expires_at<now()-interval '1 day'; INSERT INTO a1admin.sessions(token_hash,user_id,expires_at) VALUES (@hash,@id,now()+interval '30 minutes')",
                    "id", id, "hash", PasswordHasher.TokenHash(token)))
                    command.ExecuteNonQuery();
                Database.Audit(connection, transaction, id, "login.success", id);
                transaction.Commit();
                return token;
            }
        }

        public CurrentUser Resolve(string token)
        {
            if (string.IsNullOrEmpty(token) || token.Length > 100)
                return null;
            using (var connection = database.Open())
            using (var command = Database.Command(connection, null,
                "SELECT u.id,u.username,u.display_name,pp.permission_code FROM a1admin.sessions s JOIN a1admin.users u ON u.id=s.user_id LEFT JOIN a1admin.user_profiles up ON up.user_id=u.id LEFT JOIN a1admin.profiles p ON p.id=up.profile_id AND p.active LEFT JOIN a1admin.profile_permissions pp ON pp.profile_id=p.id WHERE s.token_hash=@hash AND s.revoked_at IS NULL AND s.expires_at>now() AND u.active",
                "hash", PasswordHasher.TokenHash(token)))
            using (var reader = command.ExecuteReader())
            {
                CurrentUser user = null;
                while (reader.Read())
                {
                    if (user == null)
                        user = new CurrentUser { Id = reader.GetInt32(0), UserName = reader.GetString(1), DisplayName = reader.GetString(2) };
                    if (!reader.IsDBNull(3))
                        user.Permissions.Add(reader.GetString(3));
                }
                return user;
            }
        }

        public void Logout(string token)
        {
            if (string.IsNullOrEmpty(token))
                return;
            using (var connection = database.Open())
            using (var transaction = connection.BeginTransaction())
            {
                using (var command = Database.Command(connection, transaction, "UPDATE a1admin.sessions SET revoked_at=now() WHERE token_hash=@hash RETURNING user_id", "hash", PasswordHasher.TokenHash(token)))
                {
                    var userId = command.ExecuteScalar();
                    if (userId != null)
                        Database.Audit(connection, transaction, (int)userId, "logout", (int)userId);
                }
                transaction.Commit();
            }
        }

        public void ChangePassword(int userId, string oldPassword, string password)
        {
            var newHash = PasswordHasher.Hash(password);
            using (var connection = database.Open())
            using (var transaction = connection.BeginTransaction())
            {
                using (var command = Database.Command(connection, transaction, "SELECT password_hash FROM a1admin.users WHERE id=@id AND active FOR UPDATE", "id", userId))
                    if (!PasswordHasher.Verify(oldPassword, command.ExecuteScalar() as string))
                        throw new RuleException("La contraseña actual es incorrecta.");
                using (var command = Database.Command(connection, transaction, "UPDATE a1admin.users SET password_hash=@hash,updated_at=now() WHERE id=@id; UPDATE a1admin.sessions SET revoked_at=now() WHERE user_id=@id", "id", userId, "hash", newHash))
                    command.ExecuteNonQuery();
                Database.Audit(connection, transaction, userId, "password.changed", userId);
                transaction.Commit();
            }
        }
    }
}
