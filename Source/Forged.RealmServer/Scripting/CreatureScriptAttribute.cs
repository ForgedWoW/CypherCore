// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.RealmServer.Scripting;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class CreatureScriptAttribute : ScriptAttribute
{
	public uint[] CreatureIds { get; private set; }

	public CreatureScriptAttribute(params uint[] creatureIds)
	{
		CreatureIds = creatureIds;
	}

	public CreatureScriptAttribute(string name = "", params object[] args) : base(name, args) { }

	public CreatureScriptAttribute(uint creatureId, string name = "", params object[] args) : base(name, args)
	{
		CreatureIds = new[]
		{
			creatureId
		};
	}

	public CreatureScriptAttribute(uint[] creatureIds, string name = "", params object[] args) : base(name, args)
	{
		CreatureIds = creatureIds;
	}
}