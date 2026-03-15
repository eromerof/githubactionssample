using System.Text.Json;

namespace PluginsDataverse.UITests
{
    internal static class TestConfig
    {
        private static readonly JsonElement _config;

        static TestConfig()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(path))
                throw new FileNotFoundException($"No se encontró appsettings.json en: {path}. Cópialo desde appsettings.template.json y rellena los valores.");

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            _config = doc.RootElement.GetProperty("Dataverse").Clone();
        }

        public static string OrgUrl => _config.GetProperty("OrgUrl").GetString()!;
        public static string AppId => _config.GetProperty("AppId").GetString()!;
        public static string EntityName => _config.GetProperty("EntityName").GetString()!;
        public static string Username => _config.GetProperty("Username").GetString()!;
        public static string Password => _config.GetProperty("Password").GetString()!;
        public static string MfaSecretKey => _config.GetProperty("MfaSecretKey").GetString()!.ToUpper();
        public static string ValidDni => _config.GetProperty("ValidDni").GetString()!;
        public static string InvalidDni => _config.GetProperty("InvalidDni").GetString()!;
    }
}
