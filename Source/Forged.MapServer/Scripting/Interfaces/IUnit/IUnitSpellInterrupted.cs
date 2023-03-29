﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Spells;

namespace Forged.MapServer.Scripting.Interfaces.IUnit;

public interface IUnitSpellInterrupted : IScriptObject
{
    void SpellInterrupted(Spell spellInterrupted, Spell interruptedBy);
}