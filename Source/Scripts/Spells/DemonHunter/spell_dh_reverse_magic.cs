// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

[SpellScript(205604)]
public class SpellDhReverseMagic : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        var player = Caster;

        if (player == null || !player.AsPlayer)
            return;

        Unit player = player.AsPlayer;

        var allies = new List<Unit>();
        var check = new AnyFriendlyUnitInObjectRangeCheck(player, player, 10.0f, true);
        var searcher = new UnitListSearcher(player, allies, check, GridType.All);
        Cell.VisitGrid(player, searcher, 10.0f);

        foreach (var unit in allies)
        {
            var auraListToRemove = new SortedSet<AuraData>();
            var auraList = unit.GetAppliedAurasQuery();

            foreach (var iter in auraList.IsPositive(false).GetResults())
            {
                var aura = iter.Base;

                if (aura == null)
                    continue;

                var caster = aura.Caster;

                if (caster == null || caster.GUID == unit.GUID)
                    continue;

                if (!caster.IsWithinDist(unit, 40.0f))
                    continue;

                if (aura.SpellInfo.Dispel != DispelType.Magic)
                    continue;

                var creature = caster.AsCreature;

                if (creature != null)
                {
                    if (creature.Template.Rank == CreatureEliteType.WorldBoss)
                        continue;

                    if (creature.Template.Rank == CreatureEliteType.Elite && creature.Map.IsDungeon)
                        continue;
                }

                var targetAura = unit.AddAura(aura.Id, caster);

                if (targetAura != null)
                {
                    foreach (var aurEff in targetAura.AuraEffects)
                    {
                        targetAura.SetMaxDuration(aura.MaxDuration);
                        targetAura.SetDuration(aura.Duration);

                        if (aura.GetEffect(aurEff.Key) != null)
                        {
                            var auraEffect = unit.GetAuraEffect(aura.Id, aurEff.Key);

                            if (auraEffect == null)
                                continue;

                            var amount = auraEffect.Amount;

                            if (auraEffect.AuraType == AuraType.PeriodicDamage || auraEffect.AuraType == AuraType.PeriodicDamagePercent)
                                amount = (int)caster.SpellDamageBonusDone(unit, aura.SpellInfo, amount, DamageEffectType.DOT, aura.SpellInfo.Effects[aurEff.Key], auraEffect.Base.StackAmount, Spell);

                            //targetAura->GetEffect(i)->VariableStorage.Set("DontRecalculatePerodics", true);
                            aurEff.Value.SetAmount(amount);
                            aurEff.Value.SetPeriodicTimer(auraEffect.GetPeriodicTimer());
                        }
                    }

                    targetAura.SetNeedClientUpdateForTargets();
                }

                auraListToRemove.Add(new AuraData(aura.Id, caster.GUID));
            }

            foreach (var aura in auraListToRemove)
                unit.RemoveAura(aura.MID, aura.MCasterGuid);

            auraListToRemove.Clear();
        }
    }
}