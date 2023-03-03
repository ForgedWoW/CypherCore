// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections;
using Game.Scripting.Interfaces.ISpellManager;
using Game.Spells;

namespace Scripts.SpellFixes.Warlock
{
    public class BuringRushSpellFix : ISpellManagerSpellLateFix
    {
        public int[] SpellIds => new[] { 111400 };

        public void ApplySpellFix(SpellInfo spellInfo)
        {
            spellInfo.NegativeEffects = new BitSet(spellInfo.GetEffects().Count); // no negitive effects for burning rush
        }
    }
}
