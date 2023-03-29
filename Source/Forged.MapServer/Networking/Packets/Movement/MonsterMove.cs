// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Numerics;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Movement;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

public class MonsterMove : ServerPacket
{
    public MovementMonsterSpline SplineData;
    public ObjectGuid MoverGUID;
    public Vector3 Pos;

    public MonsterMove() : base(ServerOpcodes.OnMonsterMove, ConnectionType.Instance)
    {
        SplineData = new MovementMonsterSpline();
    }

    public void InitializeSplineData(MoveSpline moveSpline)
    {
        SplineData.Id = moveSpline.GetId();
        var movementSpline = SplineData.Move;

        var splineFlags = moveSpline.splineflags;
        splineFlags.SetUnsetFlag(SplineFlag.Cyclic, moveSpline.IsCyclic());
        movementSpline.Flags = (uint)(splineFlags.Flags & ~SplineFlag.MaskNoMonsterMove);
        movementSpline.Face = moveSpline.facing.type;
        movementSpline.FaceDirection = moveSpline.facing.angle;
        movementSpline.FaceGUID = moveSpline.facing.target;
        movementSpline.FaceSpot = moveSpline.facing.f;

        if (splineFlags.HasFlag(SplineFlag.Animation))
        {
            MonsterSplineAnimTierTransition animTierTransition = new()
            {
                TierTransitionID = (int)moveSpline.anim_tier.TierTransitionId,
                StartTime = (uint)moveSpline.effect_start_time,
                AnimTier = moveSpline.anim_tier.AnimTier
            };

            movementSpline.AnimTierTransition = animTierTransition;
        }

        movementSpline.MoveTime = (uint)moveSpline.Duration();

        if (splineFlags.HasFlag(SplineFlag.Parabolic) && (moveSpline.spell_effect_extra == null || moveSpline.effect_start_time != 0))
        {
            MonsterSplineJumpExtraData jumpExtraData = new()
            {
                JumpGravity = moveSpline.vertical_acceleration,
                StartTime = (uint)moveSpline.effect_start_time
            };

            movementSpline.JumpExtraData = jumpExtraData;
        }

        if (splineFlags.HasFlag(SplineFlag.FadeObject))
            movementSpline.FadeObjectTime = (uint)moveSpline.effect_start_time;

        if (moveSpline.spell_effect_extra != null)
        {
            MonsterSplineSpellEffectExtraData spellEffectExtraData = new()
            {
                TargetGuid = moveSpline.spell_effect_extra.Target,
                SpellVisualID = moveSpline.spell_effect_extra.SpellVisualId,
                ProgressCurveID = moveSpline.spell_effect_extra.ProgressCurveId,
                ParabolicCurveID = moveSpline.spell_effect_extra.ParabolicCurveId,
                JumpGravity = moveSpline.vertical_acceleration
            };

            movementSpline.SpellEffectExtraData = spellEffectExtraData;
        }

        lock (moveSpline.spline)
        {
            var spline = moveSpline.spline;
            var array = spline.GetPoints();

            if (splineFlags.HasFlag(SplineFlag.UncompressedPath))
            {
                if (!splineFlags.HasFlag(SplineFlag.Cyclic))
                {
                    var count = spline.GetPointCount() - 3;

                    for (uint i = 0; i < count; ++i)
                        movementSpline.Points.Add(array[i + 2]);
                }
                else
                {
                    var count = spline.GetPointCount() - 3;
                    movementSpline.Points.Add(array[1]);

                    for (uint i = 0; i < count; ++i)
                        movementSpline.Points.Add(array[i + 1]);
                }
            }
            else
            {
                var lastIdx = spline.GetPointCount() - 3;
                var realPath = new Span<Vector3>(spline.GetPoints()).Slice(1);

                movementSpline.Points.Add(realPath[lastIdx]);

                if (lastIdx > 1)
                {
                    var middle = (realPath[0] + realPath[lastIdx]) / 2.0f;

                    // first and last points already appended
                    for (var i = 1; i < lastIdx; ++i)
                        movementSpline.PackedDeltas.Add(middle - realPath[i]);
                }
            }
        }
    }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(MoverGUID);
        _worldPacket.WriteVector3(Pos);
        SplineData.Write(_worldPacket);
    }
}