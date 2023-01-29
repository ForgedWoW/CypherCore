﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;
using Game.Entities;

namespace Game.Movement
{
    internal class GenericMovementGenerator : MovementGenerator
    {
        private readonly uint _arrivalSpellId;
        private readonly TimeTracker _duration;
        private readonly uint _pointId;
        private readonly Action<MoveSplineInit> _splineInit;
        private readonly MovementGeneratorType _type;
        private ObjectGuid _arrivalSpellTargetGuid;

        public GenericMovementGenerator(Action<MoveSplineInit> initializer, MovementGeneratorType type, uint id, uint arrivalSpellId = 0, ObjectGuid arrivalSpellTargetGuid = default)
        {
            _splineInit = initializer;
            _type = type;
            _pointId = id;
            _duration = new TimeTracker();
            _arrivalSpellId = arrivalSpellId;
            _arrivalSpellTargetGuid = arrivalSpellTargetGuid;

            Mode = MovementGeneratorMode.Default;
            Priority = MovementGeneratorPriority.Normal;
            Flags = MovementGeneratorFlags.InitializationPending;
            BaseUnitState = UnitState.Roaming;
        }

        public override void Initialize(Unit owner)
        {
            if (HasFlag(MovementGeneratorFlags.Deactivated) &&
                !HasFlag(MovementGeneratorFlags.InitializationPending)) // Resume spline is not supported
            {
                RemoveFlag(MovementGeneratorFlags.Deactivated);
                AddFlag(MovementGeneratorFlags.Finalized);

                return;
            }

            RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Deactivated);
            AddFlag(MovementGeneratorFlags.Initialized);

            MoveSplineInit init = new(owner);
            _splineInit(init);
            _duration.Reset((uint)init.Launch());
        }

        public override void Reset(Unit owner)
        {
            Initialize(owner);
        }

        public override bool Update(Unit owner, uint diff)
        {
            if (!owner ||
                HasFlag(MovementGeneratorFlags.Finalized))
                return false;

            // Cyclic splines never expire, so update the duration only if it's not cyclic
            if (!owner.MoveSpline.IsCyclic())
                _duration.Update(diff);

            if (_duration.Passed() ||
                owner.MoveSpline.Finalized())
            {
                AddFlag(MovementGeneratorFlags.InformEnabled);

                return false;
            }

            return true;
        }

        public override void Deactivate(Unit owner)
        {
            AddFlag(MovementGeneratorFlags.Deactivated);
        }

        public override void Finalize(Unit owner, bool active, bool movementInform)
        {
            AddFlag(MovementGeneratorFlags.Finalized);

            if (movementInform && HasFlag(MovementGeneratorFlags.InformEnabled))
                MovementInform(owner);
        }

        public override MovementGeneratorType GetMovementGeneratorType()
        {
            return _type;
        }

        private void MovementInform(Unit owner)
        {
            if (_arrivalSpellId != 0)
                owner.CastSpell(Global.ObjAccessor.GetUnit(owner, _arrivalSpellTargetGuid), _arrivalSpellId, true);

            Creature creature = owner.ToCreature();

            if (creature != null &&
                creature.GetAI() != null)
                creature.GetAI().MovementInform(_type, _pointId);
        }
    }
}