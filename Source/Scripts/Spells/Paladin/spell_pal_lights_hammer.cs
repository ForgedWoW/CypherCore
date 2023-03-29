// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin;

// Light's Hammer - 122773
[SpellScript(122773)]
public class spell_pal_lights_hammer : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        var caster = Caster;

        if (caster != null)
        {
            var tempList = new List<Creature>();
            var LightsHammerlist = new List<Creature>();

            LightsHammerlist = caster.GetCreatureListWithEntryInGrid(PaladinNPCs.NPC_PALADIN_LIGHTS_HAMMER, 200.0f);

            tempList = new List<Creature>(LightsHammerlist);

            for (var i = tempList.GetEnumerator(); i.MoveNext();)
            {
                var owner = i.Current.OwnerUnit;

                if (owner != null && owner.GUID == caster.GUID && i.Current.IsSummon)
                    continue;

                LightsHammerlist.Remove(i.Current);
            }

            foreach (var item in LightsHammerlist)
                item.CastSpell(item, PaladinSpells.LightHammerPeriodic, true);
        }
    }
}