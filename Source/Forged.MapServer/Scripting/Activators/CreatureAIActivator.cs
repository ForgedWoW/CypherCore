// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.AI.SmartScripts;
using Forged.MapServer.Scripting.BaseScripts;
using Forged.MapServer.Scripting.Interfaces;

namespace Forged.MapServer.Scripting.Activators;

public class CreatureAIActivator : IScriptActivator
{
	public List<string> ScriptBaseTypes => new()
	{
		nameof(ScriptedAI),
		nameof(BossAI),
		nameof(CreatureAI),
		nameof(TurretAI),
		nameof(ArcherAI),
		nameof(AggressorAI),
		nameof(NullCreatureAI),
		nameof(PassiveAI),
		nameof(PetAI),
		nameof(ReactorAI),
		nameof(ScheduledChangeAI),
		nameof(SmartAI),
		nameof(VehicleAI),
		nameof(CasterAI)
	};

	public IScriptObject Activate(Type type, string name, ScriptAttribute attribute)
	{
		return (IScriptObject)Activator.CreateInstance(typeof(GenericCreatureScript<>).MakeGenericType(type), name, attribute.Args);
	}
}