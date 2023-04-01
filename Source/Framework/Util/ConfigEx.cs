// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Microsoft.Extensions.Configuration;

namespace Framework.Util;

public static class ConfigEx
{
    public static T GetDefaultValue<T>(this IConfiguration config, string key, T defaultValue)
    {
        var keys = key.Split('.');
        var section = config as IConfigurationSection;

        foreach (var k in keys)
        {
            section = section?.GetSection(k);
        }

        var value = section?.Value;

        if (value == null)
            return defaultValue;

        return (T)Convert.ChangeType(value, typeof(T));
    }
}