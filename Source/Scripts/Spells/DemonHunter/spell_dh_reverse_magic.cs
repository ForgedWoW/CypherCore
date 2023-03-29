// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(205604)]
public class spell_dh_reverse_magic : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        var player = Caster;

        if (player == null || !player.AsPlayer)
            return;

        Unit _player = player.AsPlayer;

        var allies = new List<Unit>();
        var check = new AnyFriendlyUnitInObjectRangeCheck(_player, _player, 10.0f, true);
        var searcher = new UnitListSearcher(_player, allies, check, GridType.All);
        Cell.VisitGrid(_player, searcher, 10.0f);

        foreach (var unit in allies)
        {
            var auraListToRemove = new SortedSet<auraData>();
            var AuraList = unit.GetAppliedAurasQuery();

            foreach (var iter in AuraList.IsPositive(false).GetResults())
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

                auraListToRemove.Add(new auraData(aura.Id, caster.GUID));
            }

            foreach (var aura in auraListToRemove)
                unit.RemoveAura(aura.m_id, aura.m_casterGuid);

            auraListToRemove.Clear();
        }
    }
}