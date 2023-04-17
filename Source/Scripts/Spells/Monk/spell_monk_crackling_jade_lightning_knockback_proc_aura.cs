// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[Script] // 117959 - Crackling Jade Lightning
internal class SpellMonkCracklingJadeLightningKnockbackProcAura : AuraScript, IAuraCheckProc, IHasAuraEffects
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
        Target.SpellFactory.CastSpell(eventInfo.Actor, MonkSpells.CracklingJadeLightningKnockback, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
        Target.SpellFactory.CastSpell(Target, MonkSpells.CracklingJadeLightningKnockbackCd, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
    }
}