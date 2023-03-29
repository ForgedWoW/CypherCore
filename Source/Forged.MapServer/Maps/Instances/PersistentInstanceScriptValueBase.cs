// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Maps.Instances;

public class PersistentInstanceScriptValueBase
{
    protected InstanceScript _instance;
    protected string _name;
    protected object _value;

    protected PersistentInstanceScriptValueBase(InstanceScript instance, string name, object value)
    {
        _instance = instance;
        _name = name;
        _value = value;

        _instance.RegisterPersistentScriptValue(this);
    }

    public string GetName()
    {
        return _name;
    }

    public UpdateAdditionalSaveDataEvent CreateEvent()
    {
        return new UpdateAdditionalSaveDataEvent(_name, _value);
    }

    public void LoadValue(long value)
    {
        _value = value;
    }

    public void LoadValue(double value)
    {
        _value = value;
    }
}