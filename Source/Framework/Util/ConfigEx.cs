using System;
using Microsoft.Extensions.Configuration;

namespace Framework.Util
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
