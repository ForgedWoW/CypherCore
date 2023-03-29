// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[SpellScript(155148)]
public class spell_mage_kindling : AuraScript, IHasAuraEffects, IAuraCheckProc
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public bool CheckProc(ProcEventInfo eventInfo)
    {
        return eventInfo.SpellInfo.Id == MageSpells.FIREBALL || eventInfo.SpellInfo.Id == MageSpells.FIRE_BLAST || eventInfo.SpellInfo.Id == MageSpells.PHOENIX_FLAMES;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo UnnamedParameter)
    {
        var caster = Caster;

        if (caster == null)
            return;

        caster.SpellHistory.ModifyCooldown(MageSpells.COMBUSTION, TimeSpan.FromSeconds(aurEff.Amount * -1));
    }
}