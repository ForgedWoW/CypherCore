// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.SmartScripts;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;
using Framework.Dynamic;
using Scripts.Spells.Warlock;

namespace Scripts.Pets
{
    namespace Warlock
    {
        [CreatureScript(new uint[]
        {
            11859, 59000
        })]
        // Doomguard - 11859, Terrorguard - 59000
        public class NPCWarlockDoomguard : SmartAI
        {
            public EventMap Events = new();
            public double MaxDistance;

            public NPCWarlockDoomguard(Creature creature) : base(creature)
            {
                if (!Me.TryGetOwner(out Player owner))
                    return;

                creature.SetLevel(owner.Level);
                creature.UpdateLevelDependantStats();
                creature.ReactState = ReactStates.Aggressive;
                creature.SetCreatorGUID(owner.GUID);

                var summon = creature.ToTempSummon();

                if (summon != null)
                {
                    summon.SetCanFollowOwner(true);
                    summon.MotionMaster.Clear();
                    summon.MotionMaster.MoveFollow(owner, SharedConst.PetFollowDist, summon.FollowAngle);
                    StartAttackOnOwnersInCombatWith();
                }
            }

            public override void Reset()
            {
                Me.Class = PlayerClass.Rogue;
                Me.SetPowerType(PowerType.Energy);
                Me.SetMaxPower(PowerType.Energy, 200);
                Me.SetPower(PowerType.Energy, 200);

                Events.Reset();
                Events.ScheduleEvent(1, TimeSpan.FromSeconds(3));

                Me.SetControlled(true, UnitState.Root);
                MaxDistance = SpellManager.Instance.GetSpellInfo(WarlockSpells.PET_DOOMBOLT, Difficulty.None).RangeEntry.RangeMax[0];
            }

            public override void UpdateAI(uint diff)
            {
                UpdateVictim();
                var owner = Me.OwnerUnit;

                if (Me.OwnerUnit)
                {
                    var victim = owner.Victim;

                    if (owner.Victim)
                        Me.Attack(victim, false);
                }

                Events.Update(diff);

                var eventId = Events.ExecuteEvent();

                while (eventId != 0)
                {
                    switch (eventId)
                    {
                        case 1:
                            if (!Me.Victim)
                            {
                                Me.SetControlled(false, UnitState.Root);
                                Events.ScheduleEvent(eventId, TimeSpan.FromSeconds(1));

                                return;
                            }

                            Me.SetControlled(true, UnitState.Root);
                            Me.SpellFactory.CastSpell(Me.Victim, WarlockSpells.PET_DOOMBOLT, new CastSpellExtraArgs(TriggerCastFlags.None).SetOriginalCaster(Me.OwnerGUID));
                            Events.ScheduleEvent(eventId, TimeSpan.FromSeconds(3));

                            break;
                    }

                    eventId = Events.ExecuteEvent();
                }
            }
        }
    }
}