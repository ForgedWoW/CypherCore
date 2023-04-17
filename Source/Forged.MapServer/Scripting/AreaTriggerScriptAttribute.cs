// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.MapServer.Scripting;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AreaTriggerScriptAttribute : ScriptAttribute
{
    public AreaTriggerScriptAttribute(params uint[] areaTriggerId) : base("", Array.Empty<object>())
    {
        AreaTriggerIds = areaTriggerId;
    }

    public AreaTriggerScriptAttribute(string name = "", params object[] args) : base(name, args) { }

    public AreaTriggerScriptAttribute(uint areaTriggerId, string name = "", params object[] args) : base(name, args)
    {
        AreaTriggerIds = new[]
        {
            areaTriggerId
        };
    }

    public AreaTriggerScriptAttribute(uint[] areaTriggerId, string name = "", params object[] args) : base(name, args)
    {
        AreaTriggerIds = areaTriggerId;
    }

    public uint[] AreaTriggerIds { get; private set; }
}