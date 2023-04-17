// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

// Justicar's Vengeance - 215661
[SpellScript(215661)]
public class SpellPalJusticarsVengeance : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var caster = Caster;
        var target = HitUnit;

        if (target == null)
            return;

        if (target.HasAuraType(AuraType.ModStun) || target.HasAuraWithMechanic(1 << (int)Mechanics.Stun))
        {
            var damage = HitDamage;
            MathFunctions.AddPct(ref damage, 50);

            HitDamage = damage;
            EffectValue = damage;
        }

        if (caster.HasAura(PaladinSpells.DIVINE_PURPOSE_TRIGGERRED))
            caster.RemoveAura(PaladinSpells.DIVINE_PURPOSE_TRIGGERRED);

        if (caster.HasAura(PaladinSpells.FIST_OF_JUSTICE_RETRI))
            if (caster.SpellHistory.HasCooldown(PaladinSpells.HAMMER_OF_JUSTICE))
                caster.SpellHistory.ModifyCooldown(PaladinSpells.HAMMER_OF_JUSTICE, TimeSpan.FromSeconds(-10 * Time.IN_MILLISECONDS));
    }
}