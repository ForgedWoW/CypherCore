﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting.Interfaces.ISpellManager;
using Forged.MapServer.Spells;

namespace Scripts.SpellFixes.Warlock;

public class BuringRushSpellFix : ISpellManagerSpellLateFix
{
    public int[] SpellIds => new[]
    {
        111400
    };

    public void ApplySpellFix(SpellInfo spellInfo)
    {
        spellInfo.NegativeEffects = new HashSet<int>(); // no negitive effects for burning rush
    }
}