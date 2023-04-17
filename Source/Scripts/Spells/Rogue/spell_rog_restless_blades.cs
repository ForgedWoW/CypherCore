// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Rogue;

[Script] // 79096 - Restless Blades
internal class SpellRogRestlessBlades : AuraScript, IHasAuraEffects
{
    private static readonly uint[] Spells =
    {
        RogueSpells.AdrenalineRush, RogueSpells.BETWEEN_THE_EYES, RogueSpells.Sprint, RogueSpells.GRAPPLING_HOOK, RogueSpells.VANISH, RogueSpells.KillingSpree, RogueSpells.MARKED_FOR_DEATH, RogueSpells.DEATH_FROM_ABOVE
    };

    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        var spentCp = procInfo.ProcSpell?.GetPowerTypeCostAmount(PowerType.ComboPoints);

        if (spentCp.HasValue)
        {
            var cdExtra = (int)-((double)(aurEff.Amount * spentCp.Value) * 0.1f);

            var history = Target.SpellHistory;

            foreach (var spellId in Spells)
                history.ModifyCooldown(spellId, TimeSpan.FromSeconds(cdExtra), true);
        }
    }
}