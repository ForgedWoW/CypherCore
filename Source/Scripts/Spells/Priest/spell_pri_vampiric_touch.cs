// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 34914 - Vampiric Touch
internal class spell_pri_vampiric_touch : AuraScript, IAfterAuraDispel, IAuraCheckProc, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public void HandleDispel(DispelInfo dispelInfo)
    {
        var caster = Caster;

        if (caster)
        {
            var target = OwnerAsUnit;

            if (target)
            {
                var aurEff = GetEffect(1);

                if (aurEff != null)
                {
                    // backfire Damage
                    CastSpellExtraArgs args = new(aurEff);
                    args.AddSpellMod(SpellValueMod.BasePoint0, aurEff.Amount * 8);
                    caster.CastSpell(target, PriestSpells.VAMPIRIC_TOUCH_DISPEL, args);
                }
            }
        }
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 2, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    public bool CheckProc(ProcEventInfo eventInfo)
    {
        return eventInfo.ProcTarget == Caster;
    }

    private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        eventInfo.ProcTarget.CastSpell((Unit)null, PriestSpells.GEN_REPLENISHMENT, new CastSpellExtraArgs(aurEff));
    }
}