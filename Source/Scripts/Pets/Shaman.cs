// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.Pets
{
    namespace Shaman
    {
        internal struct SpellIds
        {
            //npc_pet_shaman_earth_elemental
            public const uint ANGERED_EARTH = 36213;

            //npc_pet_shaman_fire_elemental
            public const uint FIRE_BLAST = 57984;
            public const uint FIRE_NOVA = 12470;
            public const uint FIRE_SHIELD = 13376;
        }

        [Script]
        internal class NPCPetShamanEarthElemental : ScriptedAI
        {
            public NPCPetShamanEarthElemental(Creature creature) : base(creature) { }

            public override void Reset()
            {
                Scheduler.CancelAll();

                Scheduler.Schedule(TimeSpan.FromSeconds(0),
                                   task =>
                                   {
                                       DoCastVictim(SpellIds.ANGERED_EARTH);
                                       task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20));
                                   });
            }

            public override void UpdateAI(uint diff)
            {
                if (!UpdateVictim())
                    return;

                Scheduler.Update(diff);

                DoMeleeAttackIfReady();
            }
        }

        [Script]
        public class NPCPetShamanFireElemental : ScriptedAI
        {
            public NPCPetShamanFireElemental(Creature creature) : base(creature) { }

            public override void Reset()
            {
                Scheduler.CancelAll();

                Scheduler.Schedule(TimeSpan.FromSeconds(5),
                                   TimeSpan.FromSeconds(20),
                                   task =>
                                   {
                                       DoCastVictim(SpellIds.FIRE_NOVA);
                                       task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20));
                                   });

                Scheduler.Schedule(TimeSpan.FromSeconds(5),
                                   TimeSpan.FromSeconds(20),
                                   task =>
                                   {
                                       DoCastVictim(SpellIds.FIRE_SHIELD);
                                       task.Repeat(TimeSpan.FromSeconds(2));
                                   });

                Scheduler.Schedule(TimeSpan.FromSeconds(0),
                                   task =>
                                   {
                                       DoCastVictim(SpellIds.FIRE_BLAST);
                                       task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20));
                                   });
            }

            public override void UpdateAI(uint diff)
            {
                if (!UpdateVictim())
                    return;

                Scheduler.Update(diff);

                if (Me.HasUnitState(UnitState.Casting))
                    return;

                DoMeleeAttackIfReady();
            }
        }
    }
}