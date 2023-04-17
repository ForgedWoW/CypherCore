// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Maps.Instances;

public class PersistentInstanceScriptValueBase
{
    protected InstanceScript Instance;
    protected string Name;
    protected object Value;

    protected PersistentInstanceScriptValueBase(InstanceScript instance, string name, object value)
    {
        Instance = instance;
        Name = name;
        Value = value;

        Instance.RegisterPersistentScriptValue(this);
    }

    public UpdateAdditionalSaveDataEvent CreateEvent()
    {
        return new UpdateAdditionalSaveDataEvent(Name, Value);
    }

    public string GetName()
    {
        return Name;
    }

    public void LoadValue(long value)
    {
        Value = value;
    }

    public void LoadValue(double value)
    {
        Value = value;
    }
}