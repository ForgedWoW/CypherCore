﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Spells;

namespace Forged.RealmServer.Scripting.Interfaces.IUnit;

public interface IUnitSpellInterrupted : IScriptObject
{
	void SpellInterrupted(Spell spellInterrupted, Spell interruptedBy);
}