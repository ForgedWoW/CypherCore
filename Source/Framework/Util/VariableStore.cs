// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Framework.Util;

public class VariableStore
{
    private readonly Dictionary<string, object> _variables = new();

    public T GetValue<T>(string key, T defaultValue)
    {
        lock (_variables)
        {
            if (_variables.TryGetValue(key, out var val) && typeof(T) == val.GetType())
                return (T)val;
        }

        return defaultValue;
    }

    public void Set<T>(string key, T objectVal)
    {
        lock (_variables)
        {
            _variables[key] = objectVal;
        }
    }

    public bool Exist(string key)
    {
        return _variables.ContainsKey(key);
    }

    public void Remove(string key)
    {
        _variables.Remove(key);
    }
}