// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[Script] // 210155 - Death Sweep
internal class SpellDhBladeDanceDamage : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var damage = HitDamage;

        var aurEff = Caster.GetAuraEffect(DemonHunterSpells.FirstBlood, 0);

        if (aurEff != null)
        {
            var script = aurEff.Base.GetScript<SpellDhFirstBlood>();

            if (script != null)
                if (HitUnit.GUID == script.GetFirstTarget())
                    MathFunctions.AddPct(ref damage, aurEff.Amount);
        }

        HitDamage = damage;
    }
}