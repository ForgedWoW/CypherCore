// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[Script] // 186263 - Shadow Mend
internal class SpellPriShadowMend : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        var target = HitUnit;

        if (target != null)
        {
            var caster = Caster;

            var periodicAmount = HitHeal / 20;
            var damageForAuraRemoveAmount = periodicAmount * 10;

            if (caster.HasAura(PriestSpells.ATONEMENT) &&
                !caster.HasAura(PriestSpells.TRINITY))
                caster.SpellFactory.CastSpell(target, PriestSpells.ATONEMENT_TRIGGERED, new CastSpellExtraArgs(Spell));

            // Handle Masochism talent
            if (caster.HasAura(PriestSpells.MASOCHISM_TALENT) &&
                caster.GUID == target.GUID)
            {
                caster.SpellFactory.CastSpell(caster, PriestSpells.MASOCHISM_PERIODIC_HEAL, new CastSpellExtraArgs(Spell).AddSpellMod(SpellValueMod.BasePoint0, periodicAmount));
            }
            else if (target.IsInCombat &&
                     periodicAmount != 0)
            {
                CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                args.SetTriggeringSpell(Spell);
                args.AddSpellMod(SpellValueMod.BasePoint0, periodicAmount);
                args.AddSpellMod(SpellValueMod.BasePoint1, damageForAuraRemoveAmount);
                caster.SpellFactory.CastSpell(target, PriestSpells.SHADOW_MEND_PERIODIC_DUMMY, args);
            }
        }
    }
}