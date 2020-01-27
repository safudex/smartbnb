using Microsoft.Extensions.Configuration;
using Neo.Network.P2P;
using System.Linq;
using System.Reflection;

namespace Neo.Plugins
{
    internal class Settings
    {
        public string RedisHost { get; }
        public string RedisPort { get; }

        public static Settings Default { get; private set; }

        private Settings(IConfigurationSection section)
        {
            this.RedisHost = section.GetSection("RedisHost").Value;
            this.RedisPort = section.GetSection("RedisPort").Value;
        }
        public static void Load(IConfigurationSection section)
        {
            Default = new Settings(section);
        }
    }
}
