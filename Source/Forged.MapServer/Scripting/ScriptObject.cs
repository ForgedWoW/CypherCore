// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting.Interfaces;

namespace Forged.MapServer.Scripting;

public abstract class ScriptObject : IScriptObject
{
    private readonly string _name;

    public ScriptObject(string name)
    {
        _name = name;
    }

    public static T GetInstanceAI<T>(WorldObject obj) where T : class
    {
        var instance = obj.Location.Map.ToInstanceMap;

        if (instance is { InstanceScript: { } })
            return (T)Activator.CreateInstance(typeof(T),
                                               new object[]
                                               {
                                                   obj
                                               });

        return null;
    }

    public string GetName()
    {
        return _name;
    }

    // It indicates whether or not this script Type must be assigned in the database.
    public virtual bool IsDatabaseBound()
    {
        return false;
    }
}

public abstract class ScriptObjectAutoAdd : ScriptObject
{
    protected ScriptObjectAutoAdd(string name) : base(name)
    {
        Global.ScriptMgr.AddScript(this);
    }
}

public abstract class ScriptObjectAutoAddDBBound : ScriptObject
{
    protected ScriptObjectAutoAddDBBound(string name) : base(name)
    {
        Global.ScriptMgr.AddScript(this);
    }

    public override bool IsDatabaseBound()
    {
        return true;
    }
}