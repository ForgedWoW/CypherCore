// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

// 267344 - Art of War
[SpellScript(267344)]
internal class SpellPalArtOfWar : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraCheckEffectProcHandler(CheckProc, 0, AuraType.Dummy));
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        return RandomHelper.randChance(aurEff.Amount);
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        Target.SpellHistory.ResetCooldown(PaladinSpells.BLADE_OF_JUSTICE, true);
        Target.SpellFactory.CastSpell(Target, PaladinSpells.ART_OF_WAR_TRIGGERED, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));
    }
}