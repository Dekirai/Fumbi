using Hjson;
using Newtonsoft.Json;
using System;
using System.IO;

namespace Fumbi
{
    public class Config
    {
        private static readonly string save_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.hjson");

        static Config()
        {
            if (!File.Exists(save_path))
            {
                Instance = new Config();
                Instance.Save();
                return;
            }

            using (var stream = new FileStream(save_path, FileMode.Open, FileAccess.Read))
            {
                Instance = JsonConvert.DeserializeObject<Config>(HjsonValue.Load(stream).ToString(Stringify.Plain));
            }
        }

        public Config()
        {
            BotToken = "";
            Database = new DatabaseConfig();
            OwnerId = 0;
        }

        public static Config Instance { get; }
        [JsonProperty("bot_token")] public string BotToken { get; set; }
        [JsonProperty("database")] public DatabaseConfig Database { get; set; }

        private void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.None);
            File.WriteAllText(save_path, JsonValue.Parse(json).ToString(Stringify.Hjson));
        }
        public class DatabaseConfig
        {
            public DatabaseConfig()
            {
                Host = "localhost";
            }

            [JsonProperty("host")] public string Host { get; set; }
            [JsonProperty("username")] public string Username { get; set; }
            [JsonProperty("password")] public string Password { get; set; }
            [JsonProperty("database")] public string Database { get; set; }
        }

        [JsonProperty("owner")] public ulong OwnerId { get; set; }
    }
}