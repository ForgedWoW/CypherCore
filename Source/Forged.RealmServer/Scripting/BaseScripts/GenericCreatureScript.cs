// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.RealmServer.AI;
using Game.Entities;
using Forged.RealmServer.Scripting.Interfaces.ICreature;

namespace Forged.RealmServer.Scripting.BaseScripts;

public class GenericCreatureScript<AI> : ScriptObjectAutoAddDBBound, ICreatureGetAI where AI : CreatureAI
{
	private readonly object[] _args;

	public GenericCreatureScript(string name, object[] args) : base(name)
	{
		_args = args;
	}

	public virtual CreatureAI GetAI(Creature me)
	{
		if (me.InstanceScript != null)
			return GetInstanceAI<AI>(me);
		else
			return (AI)Activator.CreateInstance(typeof(AI),
												new object[]
												{
													me
												}.Combine(_args));
	}
}