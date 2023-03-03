// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin
{
    // 31935 - Avenger's Shield
    [SpellScript(31935)]
    public class spell_pal_avengers_shield : SpellScript, ISpellOnHit
    {
        public void OnHit()
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (target == null)
            {
                return;
            }

            if (caster.HasAura(PaladinSpells.GRAND_CRUSADER_PROC))
            {
                caster.RemoveAura(PaladinSpells.GRAND_CRUSADER_PROC);
            }

            var damage = GetHitDamage();

            if (caster.HasAura(PaladinSpells.FIRST_AVENGER))
            {
                MathFunctions.AddPct(ref damage, 50);
            }

            SetHitDamage(damage);
        }
    }
}
