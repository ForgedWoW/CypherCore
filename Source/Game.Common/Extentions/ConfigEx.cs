using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Game.Common.Extentions
{
    public static class ConfigEx
    {
        public static T GetDefaultValue<T>(this IConfiguration config, string key, T defaultValue)
        {
            var value = config[key];
            if (value == null)
                return defaultValue;

            return (T)Convert.ChangeType(value, typeof(T));
        }
    }
}
