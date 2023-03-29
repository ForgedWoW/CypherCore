// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[Script]
internal class spell_dk_vile_contagion : SpellScript, ISpellOnHit
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
                    Caster.CastSpell(bounce, DeathKnightSpells.FESTERING_WOUND, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.AuraStack, stacks));
                    exclude.Add(bounce);
                }
            }
        }
    }
}