// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[Script]
internal class SpellDkVileContagion : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var target = HitUnit;

        var exclude = new List<Unit>
        {
            target
        };

        if (target != null)
        {
            var pustules = target.GetAura(DeathKnightSpells.FESTERING_WOUND);

            if (pustules != null)
            {
                var stacks = pustules.StackAmount;
                var jumps = 7;

                for (var i = 0; i < jumps; i++)
                {
                    var bounce = target.SelectNearbyAllyUnit(exclude, 8f);
                    Caster.SpellFactory.CastSpell(bounce, DeathKnightSpells.FESTERING_WOUND, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.AuraStack, stacks));
                    exclude.Add(bounce);
                }
            }
        }
    }
}