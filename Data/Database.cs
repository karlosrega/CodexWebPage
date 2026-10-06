using System;
using System.Configuration;
using Npgsql;

namespace A1IntegrationsAdmin.Data
{
    /// <summary>Short-lived pooled connections. Credentials remain in local configuration.</summary>
    public sealed class Database
    {
        private readonly string connectionString;
        public Database(string connectionString = null)
        {
            this.connectionString = connectionString ?? Environment.GetEnvironmentVariable("A1ADMIN_CONNECTION_STRING")
                ?? ConfigurationManager.ConnectionStrings["A1Admin"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(this.connectionString))
                throw new InvalidOperationException("Falta la configuración local de PostgreSQL.");
        }
        public NpgsqlConnection Open()
        {
            var connection = new NpgsqlConnection(connectionString);
            try
            {
                connection.Open();
                return connection;
            }
            catch { connection.Dispose(); throw; }
        }
        public static NpgsqlCommand Command(NpgsqlConnection connection, NpgsqlTransaction transaction, string sql, params object[] parameters)
        {
            var command = new NpgsqlCommand(sql, connection, transaction);
            command.CommandTimeout = 15;
            for (int i = 0; i < parameters.Length; i += 2)
                command.Parameters.AddWithValue((string)parameters[i], parameters[i + 1] ?? DBNull.Value);
            return command;
        }
        internal static void Audit(NpgsqlConnection connection, NpgsqlTransaction transaction, int actor, string action, int? entity)
        {
            using (var command = Command(connection, transaction,
                "INSERT INTO a1admin.audit_events(actor_id,action,entity_id) VALUES(@actor,@action,@entity)",
                "actor", actor == 0 ? (object)DBNull.Value : actor, "action", action, "entity", entity.HasValue ? (object)entity.Value : DBNull.Value))
                command.ExecuteNonQuery();
        }
    }
}
