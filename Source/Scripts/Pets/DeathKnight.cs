// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Pets
{
    namespace DeathKnight
    {
        internal struct SpellIds
        {
            public const uint SUMMON_GARGOYLE1 = 49206;
            public const uint SUMMON_GARGOYLE2 = 50514;
            public const uint DISMISS_GARGOYLE = 50515;
            public const uint SANCTUARY = 54661;
        }

        [Script]
        internal class NPCPetDkEbonGargoyle : CasterAI
        {
            public NPCPetDkEbonGargoyle(Creature creature) : base(creature) { }

            public override void InitializeAI()
            {
                base.InitializeAI();
                var ownerGuid = Me.OwnerGUID;

                if (ownerGuid.IsEmpty)
                    return;

                // Find victim of Summon Gargoyle spell
                List<Unit> targets = new();
                var uCheck = new AnyUnfriendlyUnitInObjectRangeCheck(Me, Me, 30.0f, target => target.HasAura(SpellIds.SUMMON_GARGOYLE1, ownerGuid));
                var searcher = new UnitListSearcher(Me, targets, uCheck, GridType.All);
                Cell.VisitGrid(Me, searcher, 30.0f);

                foreach (var target in targets)
                {
                    Me.Attack(target, false);

                    break;
                }
            }

            public override void JustDied(Unit killer)
            {
                // Stop Feeding Gargoyle when it dies
                var owner = Me.OwnerUnit;

                if (owner)
                    owner.RemoveAura(SpellIds.SUMMON_GARGOYLE2);
            }

            // Fly away when dismissed
            public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
            {
                if (spellInfo.Id != SpellIds.DISMISS_GARGOYLE ||
                    !Me.IsAlive)
                    return;

                var owner = Me.OwnerUnit;

                if (!owner ||
                    owner != caster)
                    return;

                // Stop Fighting
                Me.SetUnitFlag(UnitFlags.NonAttackable);

                // Sanctuary
                Me.SpellFactory.CastSpell(Me, SpellIds.SANCTUARY, true);
                Me.ReactState = ReactStates.Passive;

                //! HACK: Creature's can't have MOVEMENTFLAG_FLYING
                // Fly Away
                Me.SetCanFly(true);
                Me.SetSpeedRate(UnitMoveType.Flight, 0.75f);
                Me.SetSpeedRate(UnitMoveType.Run, 0.75f);
                var x = Me.Location.X + 20 * (float)Math.Cos(Me.Location.Orientation);
                var y = Me.Location.Y + 20 * (float)Math.Sin(Me.Location.Orientation);
                var z = Me.Location.Z + 40;
                Me.MotionMaster.Clear();
                Me.MotionMaster.MovePoint(0, x, y, z);

                // Despawn as soon as possible
                Me.DespawnOrUnsummon(TimeSpan.FromSeconds(4));
            }
        }

        [Script]
        internal class NPCPetDkGuardian : AggressorAI
        {
            public NPCPetDkGuardian(Creature creature) : base(creature) { }

            public override bool CanAIAttack(Unit target)
            {
                if (!target)
                    return false;

                var owner = Me.OwnerUnit;

                if (owner && !target.IsInCombatWith(owner))
                    return false;

                return base.CanAIAttack(target);
            }
        }
    }
}