﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.AI.SmartScripts;

public class SmartAreaTriggerAI : AreaTriggerAI
{
    private readonly SmartScript _script = new();

    public SmartAreaTriggerAI(AreaTrigger areaTrigger) : base(areaTrigger) { }

    public override void OnInitialize()
    {
        GetScript().OnInitialize(At);
    }

    public override void OnUpdate(uint diff)
    {
        GetScript().OnUpdate(diff);
    }

    public override void OnUnitEnter(Unit unit)
    {
        GetScript().ProcessEventsFor(SmartEvents.AreatriggerOntrigger, unit);
    }

    public void SetTimedActionList(SmartScriptHolder e, uint entry, Unit invoker)
    {
        GetScript().SetTimedActionList(e, entry, invoker);
    }

    public SmartScript GetScript()
    {
        return _script;
    }
}