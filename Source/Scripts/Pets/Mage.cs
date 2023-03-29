﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Pets
{
    namespace Mage
    {
        internal struct SpellIds
        {
            public const uint CloneMe = 45204;
            public const uint MastersThreatList = 58838;
            public const uint MageFrostBolt = 59638;
            public const uint MageFireBlast = 59637;
        }

        internal struct MiscConst
        {
            public const uint TimerMirrorImageInit = 0;
            public const uint TimerMirrorImageFrostBolt = 4000;
            public const uint TimerMirrorImageFireBlast = 6000;
        }

        [Script]
        internal class npc_pet_mage_mirror_image : ScriptedAI
        {
            private const float CHASE_DISTANCE = 35.0f;

            private uint _fireBlastTimer = 0;

            public npc_pet_mage_mirror_image(Creature creature) : base(creature) { }

            public override void InitializeAI()
            {
                var owner = Me.OwnerUnit;

                if (owner == null)
                    return;

                // here mirror image casts on summoner spell (not present in client dbc) 49866
                // here should be Auras (not present in client dbc): 35657, 35658, 35659, 35660 selfcast by mirror images (Stats related?)
                // Clone Me!
                owner.CastSpell(Me, SpellIds.CloneMe, true);
            }

            public override void UpdateAI(uint diff)
            {
                var owner = Me.OwnerUnit;

                if (owner == null)
                {
                    Me.DespawnOrUnsummon();

                    return;
                }

                if (_fireBlastTimer != 0)
                {
                    if (_fireBlastTimer <= diff)
                        _fireBlastTimer = 0;
                    else
                        _fireBlastTimer -= diff;
                }

                if (!UpdateVictim())
                    return;

                if (Me.HasUnitState(UnitState.Casting))
                    return;

                if (_fireBlastTimer == 0)
                {
                    DoCastVictim(SpellIds.MageFireBlast);
                    _fireBlastTimer = MiscConst.TimerMirrorImageFireBlast;
                }
                else
                {
                    DoCastVictim(SpellIds.MageFrostBolt);
                }
            }

            public override bool CanAIAttack(Unit who)
            {
                var owner = Me.OwnerUnit;

                return owner &&
                       who.IsAlive &&
                       Me.IsValidAttackTarget(who) &&
                       !who.HasBreakableByDamageCrowdControlAura() &&
                       who.IsInCombatWith(owner) &&
                       CanAIAttack(who);
            }

            // Do not reload Creature templates on evade mode enter - prevent visual lost
            public override void EnterEvadeMode(EvadeReason why)
            {
                if (Me.IsInEvadeMode ||
                    !Me.IsAlive)
                    return;

                var owner = Me.CharmerOrOwner;

                Me.CombatStop(true);

                if (owner && !Me.HasUnitState(UnitState.Follow))
                {
                    Me.MotionMaster.Clear();
                    Me.MotionMaster.MoveFollow(owner, SharedConst.PetFollowDist, Me.FollowAngle);
                }
            }

            // custom UpdateVictim implementation to handle special Target selection
            // we prioritize between things that are in combat with owner based on the owner's threat to them
            private new bool UpdateVictim()
            {
                var owner = Me.OwnerUnit;

                if (owner == null)
                    return false;

                if (!Me.HasUnitState(UnitState.Casting) &&
                    !Me.IsInCombat &&
                    !owner.IsInCombat)
                    return false;

                var currentTarget = Me.Victim;

                if (currentTarget && !CanAIAttack(currentTarget))
                {
                    Me.InterruptNonMeleeSpells(true); // do not finish casting on invalid targets
                    Me.AttackStop();
                    currentTarget = null;
                }

                // don't reselect if we're currently casting anyway
                if (currentTarget && Me.HasUnitState(UnitState.Casting))
                    return true;

                Unit selectedTarget = null;
                var mgr = owner.GetCombatManager();

                if (mgr.HasPvPCombat())
                {
                    // select pvp Target
                    double minDistance = 0.0f;

                    foreach (var pair in mgr.PvPCombatRefs)
                    {
                        var target = pair.Value.GetOther(owner);

                        if (!target.IsPlayer)
                            continue;

                        if (!CanAIAttack(target))
                            continue;

                        double dist = owner.GetDistance(target);

                        if (!selectedTarget ||
                            dist < minDistance)
                        {
                            selectedTarget = target;
                            minDistance = dist;
                        }
                    }
                }

                if (!selectedTarget)
                {
                    // select pve Target
                    double maxThreat = 0.0f;

                    foreach (var pair in mgr.PvECombatRefs)
                    {
                        var target = pair.Value.GetOther(owner);

                        if (!CanAIAttack(target))
                            continue;

                        var threat = target.GetThreatManager().GetThreat(owner);

                        if (threat >= maxThreat)
                        {
                            selectedTarget = target;
                            maxThreat = threat;
                        }
                    }
                }

                if (!selectedTarget)
                {
                    EnterEvadeMode(EvadeReason.NoHostiles);

                    return false;
                }

                if (selectedTarget != Me.Victim)
                    AttackStartCaster(selectedTarget, CHASE_DISTANCE);

                return true;
            }
        }
    }
}