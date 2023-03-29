// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[Script] // 117959 - Crackling Jade Lightning
internal class spell_monk_crackling_jade_lightning_knockback_proc_aura : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public bool CheckProc(ProcEventInfo eventInfo)
    {
        if (Target.HasAura(MonkSpells.CracklingJadeLightningKnockbackCd))
            return false;

        if (eventInfo.Actor.HasAura(MonkSpells.CracklingJadeLightningChannel, Target.GUID))
            return false;

        var currentChanneledSpell = Target.GetCurrentSpell(CurrentSpellTypes.Channeled);

        if (!currentChanneledSpell ||
            currentChanneledSpell.SpellInfo.Id != MonkSpells.CracklingJadeLightningChannel)
            return false;

        return true;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        Target.CastSpell(eventInfo.Actor, MonkSpells.CracklingJadeLightningKnockback, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
        Target.CastSpell(Target, MonkSpells.CracklingJadeLightningKnockbackCd, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
    }
}