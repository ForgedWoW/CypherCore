// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

//80240 - Havoc
[SpellScript(WarlockSpells.HAVOC)]
internal class SpellWarlHavoc : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        var caster = Caster;

        if (caster == null)
            return;

        var victim = procInfo.ActionTarget;

        if (victim != null)
        {
            var target = procInfo.ProcTarget;

            if (target != null)
                if (victim != target)
                {
                    var spellInfo = aurEff.SpellInfo;

                    if (spellInfo != null)
                    {
                        var dmg = procInfo.DamageInfo.Damage;
                        var spell = new SpellNonMeleeDamage(caster, target, spellInfo, new SpellCastVisual(spellInfo.GetSpellVisual(caster), 0), SpellSchoolMask.Shadow);
                        spell.Damage = dmg;
                        spell.CleanDamage = spell.Damage;
                        caster.DealSpellDamage(spell, false);
                        caster.SendSpellNonMeleeDamageLog(spell);
                    }
                }
        }
    }
}