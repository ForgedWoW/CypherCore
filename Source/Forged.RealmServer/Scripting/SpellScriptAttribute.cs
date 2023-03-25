// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Scripting;
using System;

namespace Forged.RealmServer.Scripting;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class SpellScriptAttribute : ScriptAttribute
{
	public uint[] SpellIds { get; private set; }
	public bool AllRanks { get; private set; }

	public SpellScriptAttribute(params uint[] spellId) : base("", new object[0])
	{
		SpellIds = spellId;
	}

	public SpellScriptAttribute(string name = "", params object[] args) : base(name, args) { }

	public SpellScriptAttribute(uint spellId, string name = "", bool allRanks = false, params object[] args) : base(name, args)
	{
		SpellIds = new[]
		{
			spellId
		};

		AllRanks = allRanks;
	}

	public SpellScriptAttribute(uint[] spellId, string name = "", bool allRanks = false, params object[] args) : base(name, args)
	{
		SpellIds = spellId;
		AllRanks = allRanks;
	}
}