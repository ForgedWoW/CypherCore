// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Scripting.Interfaces.IAreaTriggerEntity;

namespace Forged.MapServer.Scripting.BaseScripts;

public class GenericAreaTriggerScript<AI> : ScriptObjectAutoAddDBBound, IAreaTriggerEntityGetAI where AI : AreaTriggerAI
{
    private readonly object[] _args;

    public GenericAreaTriggerScript(string name, object[] args) : base(name)
    {
        _args = args;
    }

    public AreaTriggerAI GetAI(AreaTrigger me)
    {
        if (me.Location.InstanceScript != null)
            return GetInstanceAI<AI>(me);
        else
            return (AI)Activator.CreateInstance(typeof(AI),
                                                new object[]
                                                {
                                                    me
                                                }.Combine(_args));
    }
}