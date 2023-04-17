// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellEtherealPetAura : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var levelDiff = (uint)Math.Abs(Target.Level - eventInfo.ProcTarget.Level);

        return levelDiff <= 9;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        List<TempSummon> minionList = new();
        OwnerAsUnit.GetAllMinionsByEntry(minionList, CreatureIds.ETHEREAL_SOUL_TRADER);

        foreach (Creature minion in minionList)
            if (minion.IsAIEnabled)
            {
                minion.AI.Talk(TextIds.SAY_STEAL_ESSENCE);
                minion.SpellFactory.CastSpell(eventInfo.ProcTarget, GenericSpellIds.STEAL_ESSENCE_VISUAL);
            }
    }
}