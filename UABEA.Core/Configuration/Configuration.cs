namespace UABEA.Core.Configuration
{
    public static class ConfigurationManager
    {
        public static ConfigurationSettings Settings { get; } = new();
    }

    public class ConfigurationSettings
    {
        public bool UseDarkTheme { get; set; } = false;
        public bool UseCpp2Il { get; set; } = true;
    }
}
