// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Numerics;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Movement;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

public static class MovementExtensions
{
    public static MovementInfo ReadMovementInfo(WorldPacket data)
    {
        var movementInfo = new MovementInfo
        {
            Guid = data.ReadPackedGuid(),
            MovementFlags = (MovementFlag)data.ReadUInt32()
        };

        movementInfo.SetMovementFlags2((MovementFlag2)data.ReadUInt32());
        movementInfo.SetExtraMovementFlags2((MovementFlags3)data.ReadUInt32());
        movementInfo.Time = data.ReadUInt32();
        var x = data.ReadFloat();
        var y = data.ReadFloat();
        var z = data.ReadFloat();
        var o = data.ReadFloat();

        movementInfo.Pos.Relocate(x, y, z, o);
        movementInfo.Pitch = data.ReadFloat();
        movementInfo.StepUpStartElevation = data.ReadFloat();

        var removeMovementForcesCount = data.ReadUInt32();

        var moveIndex = data.ReadUInt32();

        for (uint i = 0; i < removeMovementForcesCount; ++i)
            data.ReadPackedGuid();

        // ResetBitReader

        var hasTransport = data.HasBit();
        var hasFall = data.HasBit();
        var hasSpline = data.HasBit(); // todo 6.x read this infos

        data.ReadBit(); // HeightChangeFailed
        data.ReadBit(); // RemoteTimeValid
        var hasInertia = data.HasBit();
        var hasAdvFlying = data.HasBit();

        if (hasTransport)
            ReadTransportInfo(data, ref movementInfo.Transport);

        if (hasInertia)
        {
            MovementInfo.MovementInertia inertia = new()
            {
                Id = data.ReadInt32(),
                Force = data.ReadPosition(),
                Lifetime = data.ReadUInt32()
            };

            movementInfo.Inertia = inertia;
        }

        if (hasAdvFlying)
        {
            MovementInfo.AdvFlyingMovement advFlying = new()
            {
                ForwardVelocity = data.ReadFloat(),
                UpVelocity = data.ReadFloat()
            };

            movementInfo.AdvFlying = advFlying;
        }

        if (hasFall)
        {
            movementInfo.Jump.FallTime = data.ReadUInt32();
            movementInfo.Jump.Zspeed = data.ReadFloat();

            // ResetBitReader

            var hasFallDirection = data.HasBit();

            if (hasFallDirection)
            {
                movementInfo.Jump.SinAngle = data.ReadFloat();
                movementInfo.Jump.CosAngle = data.ReadFloat();
                movementInfo.Jump.Xyspeed = data.ReadFloat();
            }
        }

        return movementInfo;
    }

    public static void ReadTransportInfo(WorldPacket data, ref MovementInfo.TransportInfo transportInfo)
    {
        transportInfo.Guid = data.ReadPackedGuid(); // Transport Guid
        transportInfo.Pos.X = data.ReadFloat();
        transportInfo.Pos.Y = data.ReadFloat();
        transportInfo.Pos.Z = data.ReadFloat();
        transportInfo.Pos.Orientation = data.ReadFloat();
        transportInfo.Seat = data.ReadInt8();   // VehicleSeatIndex
        transportInfo.Time = data.ReadUInt32(); // MoveTime

        var hasPrevTime = data.HasBit();
        var hasVehicleId = data.HasBit();

        if (hasPrevTime)
            transportInfo.PrevTime = data.ReadUInt32(); // PrevMoveTime

        if (hasVehicleId)
            transportInfo.VehicleId = data.ReadUInt32(); // VehicleRecID
    }

    public static void WriteCreateObjectAreaTriggerSpline(Spline<int> spline, WorldPacket data)
    {
        data.WriteBits(spline.GetPoints().Length, 16);

        foreach (var point in spline.GetPoints())
            data.WriteVector3(point);
    }

    public static void WriteCreateObjectSplineDataBlock(MoveSpline moveSpline, WorldPacket data)
    {
        data.WriteUInt32(moveSpline.GetId()); // ID

        if (!moveSpline.IsCyclic()) // Destination
            data.WriteVector3(moveSpline.FinalDestination());
        else
            data.WriteVector3(Vector3.Zero);

        var hasSplineMove = data.WriteBit(!moveSpline.Finalized() && !moveSpline.SplineIsFacingOnly);
        data.FlushBits();

        if (hasSplineMove)
        {
            data.WriteUInt32((uint)moveSpline.Splineflags.Flags); // SplineFlags
            data.WriteInt32(moveSpline.TimePassed);              // Elapsed
            data.WriteInt32(moveSpline.Duration());               // Duration
            data.WriteFloat(1.0f);                                // DurationModifier
            data.WriteFloat(1.0f);                                // NextDurationModifier
            data.WriteBits((byte)moveSpline.Facing.type, 2);      // Face
            var hasFadeObjectTime = data.WriteBit(moveSpline.Splineflags.HasFlag(SplineFlag.FadeObject) && moveSpline.EffectStartTime < moveSpline.Duration());
            data.WriteBits(moveSpline.GetPath().Length, 16);
            data.WriteBit(false);                                 // HasSplineFilter
            data.WriteBit(moveSpline.SpellEffectExtra != null); // HasSpellEffectExtraData
            var hasJumpExtraData = data.WriteBit(moveSpline.Splineflags.HasFlag(SplineFlag.Parabolic) && (moveSpline.SpellEffectExtra == null || moveSpline.EffectStartTime != 0));
            data.WriteBit(moveSpline.AnimTier != null); // HasAnimTierTransition
            data.WriteBit(false);                        // HasUnknown901
            data.FlushBits();

            //if (HasSplineFilterKey)
            //{
            //    data << uint32(FilterKeysCount);
            //    for (var i = 0; i < FilterKeysCount; ++i)
            //    {
            //        data << float(In);
            //        data << float(Out);
            //    }

            //    data.WriteBits(FilterFlags, 2);
            //    data.FlushBits();
            //}

            switch (moveSpline.Facing.type)
            {
                case MonsterMoveType.FacingSpot:
                    data.WriteVector3(moveSpline.Facing.f); // FaceSpot

                    break;
                case MonsterMoveType.FacingTarget:
                    data.WritePackedGuid(moveSpline.Facing.target); // FaceGUID

                    break;
                case MonsterMoveType.FacingAngle:
                    data.WriteFloat(moveSpline.Facing.angle); // FaceDirection

                    break;
            }

            if (hasFadeObjectTime)
                data.WriteInt32(moveSpline.EffectStartTime); // FadeObjectTime

            foreach (var vec in moveSpline.GetPath())
                data.WriteVector3(vec);

            if (moveSpline.SpellEffectExtra != null)
            {
                data.WritePackedGuid(moveSpline.SpellEffectExtra.Target);
                data.WriteUInt32(moveSpline.SpellEffectExtra.SpellVisualId);
                data.WriteUInt32(moveSpline.SpellEffectExtra.ProgressCurveId);
                data.WriteUInt32(moveSpline.SpellEffectExtra.ParabolicCurveId);
            }

            if (hasJumpExtraData)
            {
                data.WriteFloat(moveSpline.VerticalAcceleration);
                data.WriteInt32(moveSpline.EffectStartTime);
                data.WriteUInt32(0); // Duration (override)
            }

            if (moveSpline.AnimTier != null)
            {
                data.WriteUInt32(moveSpline.AnimTier.TierTransitionId);
                data.WriteInt32(moveSpline.EffectStartTime);
                data.WriteUInt32(0);
                data.WriteUInt8(moveSpline.AnimTier.AnimTier);
            }

            //if (HasUnknown901)
            //{
            //    for (WorldPackets::Movement::MonsterSplineUnknown901::Inner const& unkInner : unk.Data) size = 16
            //    {
            //        data << int32(unkInner.Unknown_1);
            //        data << int32(unkInner.Unknown_2);
            //        data << int32(unkInner.Unknown_3);
            //        data << uint32(unkInner.Unknown_4);
            //    }
            //}
        }
    }

    public static void WriteMovementForceWithDirection(MovementForce movementForce, WorldPacket data, Position objectPosition = null)
    {
        data.WritePackedGuid(movementForce.ID);
        data.WriteVector3(movementForce.Origin);

        if (movementForce.Type == MovementForceType.Gravity && objectPosition != null) // gravity
        {
            var direction = Vector3.Zero;

            if (movementForce.Magnitude != 0.0f)
            {
                Position tmp = new(movementForce.Origin.X - objectPosition.X,
                                   movementForce.Origin.Y - objectPosition.Y,
                                   movementForce.Origin.Z - objectPosition.Z);

                var lengthSquared = tmp.GetExactDistSq(0.0f, 0.0f, 0.0f);

                if (lengthSquared > 0.0f)
                {
                    var mult = 1.0f / (float)Math.Sqrt(lengthSquared) * movementForce.Magnitude;
                    tmp.X *= mult;
                    tmp.Y *= mult;
                    tmp.Z *= mult;

                    var minLengthSquared = (tmp.X * tmp.X * 0.04f) +
                                           (tmp.Y * tmp.Y * 0.04f) +
                                           (tmp.Z * tmp.Z * 0.04f);

                    if (lengthSquared > minLengthSquared)
                        direction = new Vector3(tmp.X, tmp.Y, tmp.Z);
                }
            }

            data.WriteVector3(direction);
        }
        else
        {
            data.WriteVector3(movementForce.Direction);
        }

        data.WriteUInt32(movementForce.TransportID);
        data.WriteFloat(movementForce.Magnitude);
        data.WriteInt32(movementForce.Unused910);
        data.WriteBits((byte)movementForce.Type, 2);
        data.FlushBits();
    }

    public static void WriteMovementInfo(WorldPacket data, MovementInfo movementInfo)
    {
        var hasTransportData = !movementInfo.Transport.Guid.IsEmpty;
        var hasFallDirection = movementInfo.HasMovementFlag(MovementFlag.Falling | MovementFlag.FallingFar);
        var hasFallData = hasFallDirection || movementInfo.Jump.FallTime != 0;
        var hasSpline = false; // todo 6.x send this infos
        var hasInertia = movementInfo.Inertia.HasValue;
        var hasAdvFlying = movementInfo.AdvFlying.HasValue;

        data.WritePackedGuid(movementInfo.Guid);
        data.WriteUInt32((uint)movementInfo.MovementFlags);
        data.WriteUInt32((uint)movementInfo.GetMovementFlags2());
        data.WriteUInt32((uint)movementInfo.GetExtraMovementFlags2());
        data.WriteUInt32(movementInfo.Time);
        data.WriteFloat(movementInfo.Pos.X);
        data.WriteFloat(movementInfo.Pos.Y);
        data.WriteFloat(movementInfo.Pos.Z);
        data.WriteFloat(movementInfo.Pos.Orientation);
        data.WriteFloat(movementInfo.Pitch);
        data.WriteFloat(movementInfo.StepUpStartElevation);

        uint removeMovementForcesCount = 0;
        data.WriteUInt32(removeMovementForcesCount);

        uint moveIndex = 0;
        data.WriteUInt32(moveIndex);

        /*for (public uint i = 0; i < removeMovementForcesCount; ++i)
        {
            _worldPacket << ObjectGuid;
        }*/

        data.WriteBit(hasTransportData);
        data.WriteBit(hasFallData);
        data.WriteBit(hasSpline);
        data.WriteBit(false); // HeightChangeFailed
        data.WriteBit(false); // RemoteTimeValid
        data.WriteBit(hasInertia);
        data.WriteBit(hasAdvFlying);
        data.FlushBits();

        if (hasTransportData)
            WriteTransportInfo(data, movementInfo.Transport);

        if (hasInertia)
        {
            data.WriteInt32(movementInfo.Inertia.Value.Id);
            data.WriteXYZ(movementInfo.Inertia.Value.Force);
            data.WriteUInt32(movementInfo.Inertia.Value.Lifetime);
        }

        if (hasAdvFlying)
        {
            data.WriteFloat(movementInfo.AdvFlying.Value.ForwardVelocity);
            data.WriteFloat(movementInfo.AdvFlying.Value.UpVelocity);
        }

        if (hasFallData)
        {
            data.WriteUInt32(movementInfo.Jump.FallTime);
            data.WriteFloat(movementInfo.Jump.Zspeed);

            data.WriteBit(hasFallDirection);
            data.FlushBits();

            if (hasFallDirection)
            {
                data.WriteFloat(movementInfo.Jump.SinAngle);
                data.WriteFloat(movementInfo.Jump.CosAngle);
                data.WriteFloat(movementInfo.Jump.Xyspeed);
            }
        }
    }

    public static void WriteTransportInfo(WorldPacket data, MovementInfo.TransportInfo transportInfo)
    {
        var hasPrevTime = transportInfo.PrevTime != 0;
        var hasVehicleId = transportInfo.VehicleId != 0;

        data.WritePackedGuid(transportInfo.Guid); // Transport Guid
        data.WriteFloat(transportInfo.Pos.X);
        data.WriteFloat(transportInfo.Pos.Y);
        data.WriteFloat(transportInfo.Pos.Z);
        data.WriteFloat(transportInfo.Pos.Orientation);
        data.WriteInt8(transportInfo.Seat);   // VehicleSeatIndex
        data.WriteUInt32(transportInfo.Time); // MoveTime

        data.WriteBit(hasPrevTime);
        data.WriteBit(hasVehicleId);
        data.FlushBits();

        if (hasPrevTime)
            data.WriteUInt32(transportInfo.PrevTime); // PrevMoveTime

        if (hasVehicleId)
            data.WriteUInt32(transportInfo.VehicleId); // VehicleRecID
    }
}

//Structs