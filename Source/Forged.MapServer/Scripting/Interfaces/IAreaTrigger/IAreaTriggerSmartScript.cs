// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;

namespace Game.Scripting.Interfaces.IAreaTrigger;

public interface IAreaTriggerSmartScript : IAreaTriggerScript
{
	SmartScript _script { get; }

	public virtual void OnInitialize()
	{
		_script.OnInitialize(At);
	}

	public virtual void OnUpdate(uint diff)
	{
		_script.OnUpdate(diff);
	}

	public virtual void OnUnitEnter(Unit unit)
	{
		_script.ProcessEventsFor(SmartEvents.AreatriggerOntrigger, unit);
	}

	public virtual void SetTimedActionList(SmartScriptHolder e, uint entry, Unit invoker)
	{
		_script.SetTimedActionList(e, entry, invoker);
	}
}