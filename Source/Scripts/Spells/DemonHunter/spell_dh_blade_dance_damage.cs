// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[Script] // 210155 - Death Sweep
internal class spell_dh_blade_dance_damage : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var damage = HitDamage;

        var aurEff = Caster.GetAuraEffect(DemonHunterSpells.FirstBlood, 0);

        if (aurEff != null)
        {
            var script = aurEff.Base.GetScript<spell_dh_first_blood>();

            if (script != null)
                if (HitUnit.GUID == script.GetFirstTarget())
                    MathFunctions.AddPct(ref damage, aurEff.Amount);
        }

        HitDamage = damage;
    }
}