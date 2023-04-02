// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.AI;
using Game.Combat;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Pets
{
    namespace Hunter
    {
        internal struct SpellIds
        {
            public const uint CripplingPoison = 30981;     // Viper
            public const uint DeadlyPoisonPassive = 34657; // Venomous Snake
            public const uint MindNumbingPoison = 25810;   // Viper
        }

        internal struct CreatureIds
        {
            public const int Viper = 19921;
        }

        [Script]
        internal class npc_pet_hunter_snake_trap : ScriptedAI
        {
            private bool _isViper;
            private uint _spellTimer;

            public npc_pet_hunter_snake_trap(Creature creature) : base(creature) { }

            public override void JustEngagedWith(Unit who) { }

            public override void JustAppeared()
            {
                _isViper = Me.Entry == CreatureIds.Viper ? true : false;

                Me.SetMaxHealth((uint)(107 * (Me.Level - 40) * 0.025f));
                // Add delta to make them not all hit the same Time
                Me.SetBaseAttackTime(WeaponAttackType.BaseAttack, Me.GetBaseAttackTime(WeaponAttackType.BaseAttack) + RandomHelper.URand(0, 6) * Time.IN_MILLISECONDS);

                if (!_isViper &&
                    !Me.HasAura(SpellIds.DeadlyPoisonPassive))
                    DoCast(Me, SpellIds.DeadlyPoisonPassive, new CastSpellExtraArgs(true));
            }

            // Redefined for random Target selection:
            public override void MoveInLineOfSight(Unit who) { }

            public override void UpdateAI(uint diff)
            {
                if (Me.Victim &&
                    Me.Victim.HasBreakableByDamageCrowdControlAura())
                {
                    // don't break cc
                    Me.GetThreatManager().ClearFixate();
                    Me.InterruptNonMeleeSpells(false);
                    Me.AttackStop();

                    return;
                }

                if (Me.IsSummon &&
                    !Me.GetThreatManager().GetFixateTarget())
                {
                    // find new Target
                    var summoner = Me.ToTempSummon().GetSummonerUnit();
                    List<Unit> targets = new();

                    void addTargetIfValid(CombatReference refe)
                    {
                        var enemy = refe.GetOther(summoner);

                        if (!enemy.HasBreakableByDamageCrowdControlAura() &&
                            Me.CanCreatureAttack(enemy) &&
                            Me.IsWithinDistInMap(enemy, (float)Me.GetAttackDistance(enemy)))
                            targets.Add(enemy);
                    }

                    foreach (var pair in summoner.GetCombatManager().PvPCombatRefs)
                        addTargetIfValid(pair.Value);

                    if (targets.Empty())
                        foreach (var pair in summoner.GetCombatManager().PvECombatRefs)
                            addTargetIfValid(pair.Value);

                    foreach (var target in targets)
                        Me.EngageWithTarget(target);

                    if (!targets.Empty())
                    {
                        var target = targets.SelectRandom();
                        Me.GetThreatManager().FixateTarget(target);
                    }
                }

                if (!UpdateVictim())
                    return;

                // Viper
                if (_isViper)
                {
                    if (_spellTimer <= diff)
                    {
                        if (RandomHelper.URand(0, 2) == 0) // 33% chance to cast
                            DoCastVictim(RandomHelper.RAND(SpellIds.MindNumbingPoison, SpellIds.CripplingPoison));

                        _spellTimer = 3000;
                    }
                    else
                    {
                        _spellTimer -= diff;
                    }
                }

                DoMeleeAttackIfReady();
            }
        }
    }
}