using Dapper.FastCrud;
using MySql.Data.MySqlClient;
using Serilog;
using Serilog.Core;
using System.Data;

namespace Fumbi
{
    public static class Database
    {
        private static string s_connectionString;

        private static readonly ILogger Logger = Log.ForContext(Constants.SourceContextPropertyName, nameof(Database));

        public static void Initialize()
        {
            string server = Config.Instance.Database.Host;
            string database = Config.Instance.Database.Database;
            string uid = Config.Instance.Database.Username;
            string password = Config.Instance.Database.Password;
            s_connectionString = $"SslMode=none;Server={server};Database={database};Uid={uid};Pwd={password};Pooling=true;";

            OrmConfiguration.DefaultDialect = SqlDialect.MySql;
        }

        public static IDbConnection Open()
        {
            try
            {
                var connection = new MySqlConnection(s_connectionString);

                connection.Open();

                return connection;
            }
            catch (MySqlException ex)
            {
                switch (ex.Number)
                {
                    case 0:
                        Logger.Error("Cannot connect to the database.");
                        break;

                    case 1045:
                        Logger.Error("Invalid username/password.");
                        break;

                    default:
                        Logger.Error("Unhandled exception -> " + ex.Message);
                        break;
                }
                return null;
            }
        }
    }
}
