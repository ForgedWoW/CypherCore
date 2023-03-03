// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;

namespace Game.Movement
{
    public class PointMovementGenerator : MovementGeneratorMedium<Unit>
    {
        readonly uint _movementId;
        readonly Position _destination;
        readonly float? _speed;
        readonly bool _generatePath;

        //! if set then unit will turn to specified _orient in provided _pos
        readonly float? _finalOrient;
        readonly Unit _faceTarget;
        readonly SpellEffectExtraData _spellEffectExtra;
        readonly MovementWalkRunSpeedSelectionMode _speedSelectionMode;
        readonly float? _closeEnoughDistance;


        public uint GetId() { return _movementId; }


        public PointMovementGenerator(uint id, float x, float y, float z, bool generatePath, float speed = 0.0f, float? finalOrient = null, Unit faceTarget = null, SpellEffectExtraData spellEffectExtraData = null, MovementWalkRunSpeedSelectionMode speedSelectionMode = MovementWalkRunSpeedSelectionMode.Default, float closeEnoughDistance = 0)
        {
            _movementId = id;
            _destination = new Position(x, y, z);
            _speed = speed == 0.0f ? null : speed;
            _generatePath = generatePath;
            _finalOrient = finalOrient;
            _faceTarget = faceTarget;
            _spellEffectExtra = spellEffectExtraData;
            _closeEnoughDistance = closeEnoughDistance == 0 ? null : closeEnoughDistance;
            _speedSelectionMode = speedSelectionMode;

            Mode = MovementGeneratorMode.Default;
            Priority = MovementGeneratorPriority.Normal;
            Flags = MovementGeneratorFlags.InitializationPending;
            BaseUnitState = UnitState.Roaming;
        }

        public override void DoInitialize(Unit owner)
        {
            RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Deactivated);
            AddFlag(MovementGeneratorFlags.Initialized);

            if (_movementId == EventId.ChargePrepath)
            {
                owner.AddUnitState(UnitState.RoamingMove);
                return;
            }

            if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting())
            {
                AddFlag(MovementGeneratorFlags.Interrupted);
                owner.StopMoving();
                return;
            }

            owner.AddUnitState(UnitState.RoamingMove);

            MoveSplineInit init = new(owner);

            if (_generatePath)
            {
                PathGenerator path = new PathGenerator(owner);

                bool result = path.CalculatePath(_destination.posX, _destination.posY, _destination.posZ, false);
                if (result && (path.GetPathType() & PathType.NoPath) == 0)
                {
                    if (_closeEnoughDistance.HasValue)
                    {
                        path.ShortenPathUntilDist(_destination, _closeEnoughDistance.Value);
                    }

                    init.MovebyPath(path.GetPath());
                    return;
                }
            }

            if (_closeEnoughDistance.HasValue)
                owner.MovePosition(_destination, Math.Min(_closeEnoughDistance.Value, _destination.GetExactDist(owner)), (float)Math.PI + owner.GetRelativeAngle(_destination));

            init.MoveTo(_destination.GetPositionX(), _destination.GetPositionY(), _destination.GetPositionZ(), false);


            if (_speed.HasValue)
                init.SetVelocity(_speed.Value);

            if (_faceTarget)
                init.SetFacing(_faceTarget);

            if (_spellEffectExtra != null)
                init.SetSpellEffectExtraData(_spellEffectExtra);

            if (_finalOrient.HasValue)
                init.SetFacing(_finalOrient.Value);

            switch (_speedSelectionMode)
            {
                case MovementWalkRunSpeedSelectionMode.Default:
                    break;
                case MovementWalkRunSpeedSelectionMode.ForceRun:
                    init.SetWalk(false);
                    break;
                case MovementWalkRunSpeedSelectionMode.ForceWalk:
                    init.SetWalk(true);
                    break;
                default:
                    break;
            }

            init.Launch();

            // Call for creature group update
            Creature creature = owner.ToCreature();
            if (creature != null)
                creature.SignalFormationMovement();
        }

        public override void DoReset(Unit owner)
        {
            RemoveFlag(MovementGeneratorFlags.Transitory | MovementGeneratorFlags.Deactivated);

            DoInitialize(owner);
        }

        public override bool DoUpdate(Unit owner, uint diff)
        {
            if (owner == null)
                return false;

            if (_movementId == EventId.ChargePrepath)
            {
                if (owner.MoveSpline.Finalized())
                {
                    AddFlag(MovementGeneratorFlags.InformEnabled);
                    return false;
                }
                return true;
            }

            if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting())
            {
                AddFlag(MovementGeneratorFlags.Interrupted);
                owner.StopMoving();
                return true;
            }

            if ((HasFlag(MovementGeneratorFlags.Interrupted) && owner.MoveSpline.Finalized()) || (HasFlag(MovementGeneratorFlags.SpeedUpdatePending) && !owner.MoveSpline.Finalized()))
            {
                RemoveFlag(MovementGeneratorFlags.Interrupted | MovementGeneratorFlags.SpeedUpdatePending);

                owner.AddUnitState(UnitState.RoamingMove);

                MoveSplineInit init = new(owner);
                init.MoveTo(_destination.GetPositionX(), _destination.GetPositionY(), _destination.GetPositionZ(), _generatePath);
                if (_speed.HasValue) // Default value for point motion type is 0.0, if 0.0 spline will use GetSpeed on unit
                    init.SetVelocity(_speed.Value);
                init.Launch();

                // Call for creature group update
                Creature creature = owner.ToCreature();
                if (creature != null)
                    creature.SignalFormationMovement();
            }

            if (owner.MoveSpline.Finalized())
            {
                RemoveFlag(MovementGeneratorFlags.Transitory);
                AddFlag(MovementGeneratorFlags.InformEnabled);
                return false;
            }
            return true;
        }

        public override void DoDeactivate(Unit owner)
        {
            AddFlag(MovementGeneratorFlags.Deactivated);
            owner.ClearUnitState(UnitState.RoamingMove);
        }

        public override void DoFinalize(Unit owner, bool active, bool movementInform)
        {
            AddFlag(MovementGeneratorFlags.Finalized);
            if (active)
                owner.ClearUnitState(UnitState.RoamingMove);

            if (movementInform && HasFlag(MovementGeneratorFlags.InformEnabled) && owner.IsCreature())
                MovementInform(owner);
        }

        public void MovementInform(Unit owner)
        {
            if (owner.IsTypeId(TypeId.Unit))
            {
                if (owner.ToCreature().GetAI() != null)
                    owner.ToCreature().GetAI().MovementInform(MovementGeneratorType.Point, _movementId);
            }
        }

        public override MovementGeneratorType GetMovementGeneratorType()
        {
            return MovementGeneratorType.Point;
        }

        public override void UnitSpeedChanged()
        {
            AddFlag(MovementGeneratorFlags.SpeedUpdatePending);
        }
    }

    public class AssistanceMovementGenerator : PointMovementGenerator
    {
        public AssistanceMovementGenerator(uint id, float x, float y, float z) : base(id, x, y, z, true) { }

        public override void Finalize(Unit owner, bool active, bool movementInform)
        {
            AddFlag(MovementGeneratorFlags.Finalized);
            if (active)
                owner.ClearUnitState(UnitState.RoamingMove);

            if (movementInform && HasFlag(MovementGeneratorFlags.InformEnabled))
            {
                Creature ownerCreature = owner.ToCreature();
                ownerCreature.SetNoCallAssistance(false);
                ownerCreature.CallAssistance();
                if (ownerCreature.IsAlive())
                    ownerCreature.GetMotionMaster().MoveSeekAssistanceDistract(WorldConfig.GetUIntValue(WorldCfg.CreatureFamilyAssistanceDelay));
            }
        }

        public override MovementGeneratorType GetMovementGeneratorType() { return MovementGeneratorType.Assistance; }
    }
}
