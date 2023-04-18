// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.SmartScripts;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Scripting.Interfaces.IAreaTrigger;

public interface IAreaTriggerSmartScript : IAreaTriggerScript
{
    SmartScript _script { get; }

    public void OnInitialize()
    {
        _script.OnInitialize(At);
    }

    public void OnUnitEnter(Unit unit)
    {
        _script.ProcessEventsFor(SmartEvents.AreatriggerOntrigger, unit);
    }

    public void OnUpdate(uint diff)
    {
        _script.OnUpdate(diff);
    }

    public void SetTimedActionList(SmartScriptHolder e, uint entry, Unit invoker)
    {
        _script.SetTimedActionList(e, entry, invoker);
    }
}