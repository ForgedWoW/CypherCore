// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin;

// Light's Hammer - 122773
[SpellScript(122773)]
public class SpellPalLightsHammer : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        var caster = Caster;

        if (caster != null)
        {
            var tempList = new List<Creature>();
            var lightsHammerlist = new List<Creature>();

            lightsHammerlist = caster.GetCreatureListWithEntryInGrid(PaladinNpCs.NPC_PALADIN_LIGHTS_HAMMER, 200.0f);

            tempList = new List<Creature>(lightsHammerlist);

            for (var i = tempList.GetEnumerator(); i.MoveNext();)
            {
                var owner = i.Current.OwnerUnit;

                if (owner != null && owner.GUID == caster.GUID && i.Current.IsSummon)
                    continue;

                lightsHammerlist.Remove(i.Current);
            }

            foreach (var item in lightsHammerlist)
                item.SpellFactory.CastSpell(item, PaladinSpells.LIGHT_HAMMER_PERIODIC, true);
        }
    }
}