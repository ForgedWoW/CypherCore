﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(115175)]
public class spell_monk_soothing_mist : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.Real));
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicHeal));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }


    private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
    {
        if (!Caster)
            return;

        var target = Target;

        if (target != null)
            target.CastSpell(target, MonkSpells.SOOTHING_MIST_VISUAL, true);

        var player = Caster.AsPlayer;

        if (player != null)
            if (target != null)
            {
                var playerList = new List<Unit>();
                var tempList = new List<Creature>();
                var statueList = new List<Creature>();
                Creature statue;

                player.GetPartyMembers(playerList);

                if (playerList.Count > 1)
                {
                    playerList.Remove(target);
                    playerList.Sort(new HealthPctOrderPred());
                    playerList.Resize(1);
                }

                tempList = player.GetCreatureListWithEntryInGrid(60849, 100.0f);
                statueList = player.GetCreatureListWithEntryInGrid(60849, 100.0f);

                for (var i = tempList.GetEnumerator(); i.MoveNext();)
                {
                    var owner = i.Current.OwnerUnit;

                    if (owner != null && owner == player && i.Current.IsSummon)
                        continue;

                    statueList.Remove(i.Current);
                }

                foreach (var itr in playerList)
                    if (statueList.Count == 1)
                    {
                        statue = statueList.First();

                        if (statue.OwnerUnit != null && statue.OwnerUnit.GUID == player.GUID)
                            if (statue.OwnerUnit && statue.OwnerUnit.GUID == player.GUID)
                                statue.CastSpell(statue.OwnerUnit.AsPlayer.SelectedUnit, MonkSpells.SERPENT_STATUE_SOOTHING_MIST, false);
                    }
            }
    }

    private void HandleEffectPeriodic(AuraEffect UnnamedParameter)
    {
        var caster = Caster;

        if (caster != null)
            if (Target)
                // 25% to give 1 chi per tick
                if (RandomHelper.randChance(25))
                    caster.CastSpell(caster, MonkSpells.SOOTHING_MIST_ENERGIZE, true);
    }

    private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
    {
        if (Caster)
        {
            var target = Target;

            if (target != null)
                if (target.HasAura(MonkSpells.SOOTHING_MIST_VISUAL))
                    target.RemoveAura(MonkSpells.SOOTHING_MIST_VISUAL);
        }
    }
}