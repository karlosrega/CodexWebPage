using System;
using System.Collections.Generic;
using System.Linq;
using A1IntegrationsAdmin.Core;
using Npgsql;

namespace A1IntegrationsAdmin.Data
{
    /// <summary>Administrative writes serialize the last-administrator invariant in PostgreSQL.</summary>
    public sealed class AdminRepository
    {
        private readonly Database database;
        public AdminRepository(Database database)
        {
            this.database = database;
        }

        public PageResult<UserRecord> Users(string search, int page)
        {
            var result = new PageResult<UserRecord> { Page = Math.Max(1, page) };
            const string where = " WHERE strpos(lower(u.username||' '||u.display_name||' '||u.email),lower(@search))>0";
            using (var connection = database.Open())
            {
                using (var count = Database.Command(connection, null, "SELECT count(*) FROM a1admin.users u" + where, "search", search ?? ""))
                    result.Total = Convert.ToInt32(count.ExecuteScalar());
                result.Page = Math.Min(result.Page, result.Pages);
                using (var command = Database.Command(connection, null,
                    "SELECT u.id,u.username,u.display_name,u.email,u.active,COALESCE((SELECT string_agg(p.name,', ' ORDER BY p.name) FROM a1admin.user_profiles up JOIN a1admin.profiles p ON p.id=up.profile_id WHERE up.user_id=u.id),'') FROM a1admin.users u" + where + " ORDER BY u.id DESC LIMIT 12 OFFSET @offset", "search", search ?? "", "offset", (result.Page - 1) * 12))
                using (var reader = command.ExecuteReader())
                    while (reader.Read())
                        result.Items.Add(ReadUser(reader));
            }
            return result;
        }

        private static UserRecord ReadUser(NpgsqlDataReader reader) => new UserRecord { Id = reader.GetInt32(0), UserName = reader.GetString(1), DisplayName = reader.GetString(2), Email = reader.GetString(3), Active = reader.GetBoolean(4), Profiles = reader.GetString(5) };

        public UserRecord User(int id)
        {
            using (var connection = database.Open())
            {
                UserRecord user;
                using (var command = Database.Command(connection, null, "SELECT id,username,display_name,email,active,'' FROM a1admin.users WHERE id=@id", "id", id))
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;
                    user = ReadUser(reader);
                }
                var profiles = new List<int>();
                using (var command = Database.Command(connection, null, "SELECT profile_id FROM a1admin.user_profiles WHERE user_id=@id", "id", id))
                using (var reader = command.ExecuteReader())
                    while (reader.Read())
                        profiles.Add(reader.GetInt32(0));
                user.ProfileIds = profiles.ToArray();
                return user;
            }
        }

        public List<ProfileRecord> Profiles()
        {
            var result = new List<ProfileRecord>();
            using (var connection = database.Open())
            using (var command = Database.Command(connection, null, "SELECT p.id,p.name,p.description,p.active,p.is_system,COALESCE(array_agg(pp.permission_code) FILTER(WHERE pp.permission_code IS NOT NULL),ARRAY[]::varchar[]) FROM a1admin.profiles p LEFT JOIN a1admin.profile_permissions pp ON pp.profile_id=p.id GROUP BY p.id ORDER BY p.is_system DESC,p.name"))
            using (var reader = command.ExecuteReader())
                while (reader.Read())
                    result.Add(new ProfileRecord { Id = reader.GetInt32(0), Name = reader.GetString(1), Description = reader.GetString(2), Active = reader.GetBoolean(3), System = reader.GetBoolean(4), Permissions = reader.GetFieldValue<string[]>(5) });
            return result;
        }

        public Dictionary<string, string> Permissions()
        {
            var result = new Dictionary<string, string>();
            using (var connection = database.Open())
            using (var command = Database.Command(connection, null, "SELECT code,label FROM a1admin.permissions ORDER BY code"))
            using (var reader = command.ExecuteReader())
                while (reader.Read())
                    result.Add(reader.GetString(0), reader.GetString(1));
            return result;
        }

        private static void LockAdministration(NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            using (var command = Database.Command(connection, transaction, "SELECT pg_advisory_xact_lock(418048)"))
                command.ExecuteNonQuery();
        }

        private static void EnsureAdministrator(NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            using (var command = Database.Command(connection, transaction, "SELECT count(DISTINCT u.id) FROM a1admin.users u JOIN a1admin.user_profiles up ON up.user_id=u.id JOIN a1admin.profiles p ON p.id=up.profile_id WHERE u.active AND p.active AND p.is_system"))
                if (Convert.ToInt32(command.ExecuteScalar()) < 1)
                    throw new RuleException("Debe permanecer al menos un administrador activo.");
        }

        private static void RequirePermission(NpgsqlConnection connection, NpgsqlTransaction transaction, int actor, string permission)
        {
            using (var command = Database.Command(connection, transaction,
                "SELECT EXISTS(SELECT 1 FROM a1admin.users u JOIN a1admin.user_profiles up ON up.user_id=u.id JOIN a1admin.profiles p ON p.id=up.profile_id JOIN a1admin.profile_permissions pp ON pp.profile_id=p.id WHERE u.id=@actor AND u.active AND p.active AND pp.permission_code=@permission)", "actor", actor, "permission", permission))
                if (!(bool)command.ExecuteScalar())
                    throw new RuleException("Ya no tienes permiso para esta operación. Inicia sesión nuevamente.");
        }

        public int SaveUser(UserRecord user, string password, int actor)
        {
            ValidateUser(user);
            string hash = string.IsNullOrEmpty(password) ? null : PasswordHasher.Hash(password);
            if (user.Id == 0 && hash == null)
                throw new RuleException("Indica una contraseña para el nuevo usuario.");
            using (var connection = database.Open())
            using (var transaction = connection.BeginTransaction())
            {
                LockAdministration(connection, transaction);
                RequirePermission(connection, transaction, actor, "users.write");
                try
                {
                    using (var command = Database.Command(connection, transaction, user.Id == 0
                        ? "INSERT INTO a1admin.users(username,display_name,email,active,password_hash) VALUES(@username,@name,@email,@active,@hash) RETURNING id"
                        : "UPDATE a1admin.users SET username=@username,display_name=@name,email=@email,active=@active,password_hash=COALESCE(@hash,password_hash),updated_at=now() WHERE id=@id RETURNING id",
                        "id", user.Id, "username", user.UserName.Trim(), "name", user.DisplayName.Trim(), "email", user.Email.Trim(), "active", user.Active, "hash", hash == null ? (object)DBNull.Value : hash))
                    {
                        var savedId = command.ExecuteScalar();
                        if (savedId == null)
                            throw new RuleException("El usuario ya no existe.");
                        user.Id = (int)savedId;
                    }
                    using (var command = Database.Command(connection, transaction, "DELETE FROM a1admin.user_profiles WHERE user_id=@id; UPDATE a1admin.sessions SET revoked_at=now() WHERE user_id=@id", "id", user.Id))
                        command.ExecuteNonQuery();
                    foreach (var profile in (user.ProfileIds ?? new int[0]).Distinct())
                    {
                        using (var command = Database.Command(connection, transaction, "INSERT INTO a1admin.user_profiles(user_id,profile_id) SELECT @id,id FROM a1admin.profiles WHERE id=@profile AND active", "id", user.Id, "profile", profile))
                            if (command.ExecuteNonQuery() != 1)
                                throw new RuleException("Uno de los perfiles seleccionados no está activo o no existe.");
                    }
                    EnsureAdministrator(connection, transaction);
                    Database.Audit(connection, transaction, actor, "user.saved", user.Id);
                    transaction.Commit();
                    return user.Id;
                }
                catch (PostgresException exception) when (exception.SqlState == "23505") { throw new RuleException("El usuario o correo electrónico ya está registrado."); }
            }
        }

        public void SaveProfile(ProfileRecord profile, int actor)
        {
            if (string.IsNullOrWhiteSpace(profile.Name) || profile.Name.Trim().Length > 80 || (profile.Description ?? "").Length > 300)
                throw new RuleException("Revisa el nombre y la descripción del perfil.");
            using (var connection = database.Open())
            using (var transaction = connection.BeginTransaction())
            {
                LockAdministration(connection, transaction);
                RequirePermission(connection, transaction, actor, "profiles.write");
                if (profile.Id != 0)
                {
                    using (var command = Database.Command(connection, transaction, "SELECT is_system FROM a1admin.profiles WHERE id=@id", "id", profile.Id))
                    {
                        var system = command.ExecuteScalar();
                        if (system == null)
                            throw new RuleException("El perfil ya no existe.");
                        if ((bool)system)
                            throw new RuleException("El perfil Administrador es protegido y no puede modificarse.");
                    }
                }
                try
                {
                    using (var command = Database.Command(connection, transaction, profile.Id == 0
                        ? "INSERT INTO a1admin.profiles(name,description,active) VALUES(@name,@description,@active) RETURNING id"
                        : "UPDATE a1admin.profiles SET name=@name,description=@description,active=@active WHERE id=@id RETURNING id",
                        "id", profile.Id, "name", profile.Name.Trim(), "description", profile.Description ?? "", "active", profile.Active))
                        profile.Id = (int)command.ExecuteScalar();
                    using (var command = Database.Command(connection, transaction, "DELETE FROM a1admin.profile_permissions WHERE profile_id=@id; UPDATE a1admin.sessions SET revoked_at=now() WHERE user_id IN (SELECT user_id FROM a1admin.user_profiles WHERE profile_id=@id)", "id", profile.Id))
                        command.ExecuteNonQuery();
                    foreach (var permission in (profile.Permissions ?? new string[0]).Distinct())
                        using (var command = Database.Command(connection, transaction, "INSERT INTO a1admin.profile_permissions(profile_id,permission_code) VALUES(@id,@code)", "id", profile.Id, "code", permission))
                            command.ExecuteNonQuery();
                    EnsureAdministrator(connection, transaction);
                    Database.Audit(connection, transaction, actor, "profile.saved", profile.Id);
                    transaction.Commit();
                }
                catch (PostgresException exception) when (exception.SqlState == "23505") { throw new RuleException("Ya existe un perfil con ese nombre."); }
                catch (PostgresException exception) when (exception.SqlState == "23503") { throw new RuleException("Selecciona únicamente permisos del catálogo."); }
            }
        }

        public static void ValidateUser(UserRecord user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.UserName) || user.UserName.Trim().Length > 80 || string.IsNullOrWhiteSpace(user.DisplayName) || user.DisplayName.Trim().Length > 120 || string.IsNullOrWhiteSpace(user.Email) || user.Email.Trim().Length > 254)
                throw new RuleException("Revisa los datos obligatorios del usuario.");
            if (!System.Text.RegularExpressions.Regex.IsMatch(user.UserName.Trim(), @"^[a-zA-Z0-9._-]{3,80}$"))
                throw new RuleException("El usuario debe tener de 3 a 80 caracteres: letras, números, punto, guion o guion bajo.");
            try
            {
                var address = new System.Net.Mail.MailAddress(user.Email.Trim());
                if (address.Address != user.Email.Trim())
                    throw new FormatException();
            }
            catch (FormatException) { throw new RuleException("Indica un correo electrónico válido."); }
        }

        public int Bootstrap(string username, string name, string email, string password)
        {
            var user = new UserRecord { UserName = username, DisplayName = name, Email = email, Active = true };
            ValidateUser(user);
            var hash = PasswordHasher.Hash(password);
            using (var connection = database.Open())
            using (var transaction = connection.BeginTransaction())
            {
                LockAdministration(connection, transaction);
                using (var command = Database.Command(connection, transaction, "SELECT count(*) FROM a1admin.users"))
                    if (Convert.ToInt32(command.ExecuteScalar()) != 0)
                        throw new RuleException("La inicialización solo está permitida cuando no existen usuarios.");
                using (var command = Database.Command(connection, transaction, "INSERT INTO a1admin.users(username,display_name,email,password_hash) VALUES(@username,@name,@email,@hash) RETURNING id", "username", username, "name", name, "email", email, "hash", hash))
                    user.Id = (int)command.ExecuteScalar();
                using (var command = Database.Command(connection, transaction, "INSERT INTO a1admin.user_profiles(user_id,profile_id) SELECT @id,id FROM a1admin.profiles WHERE is_system AND active", "id", user.Id))
                    command.ExecuteNonQuery();
                EnsureAdministrator(connection, transaction);
                Database.Audit(connection, transaction, user.Id, "admin.initialized", user.Id);
                transaction.Commit();
                return user.Id;
            }
        }

        public List<Promotion> Promotions()
        {
            var result = new List<Promotion>();
            using (var connection = database.Open())
            using (var command = Database.Command(connection, null, "SELECT id,position,anchor,category,title,body,image_path,product_url FROM a1admin.promotions WHERE active ORDER BY position,id"))
            using (var reader = command.ExecuteReader())
                while (reader.Read())
                    result.Add(new Promotion { Id = reader.GetInt32(0), Position = reader.GetInt32(1), Anchor = reader.GetString(2), Category = reader.GetString(3), Title = reader.GetString(4), Body = reader.GetString(5), Image = reader.GetString(6), Link = reader.GetString(7) });
            return result;
        }
    }
}
