// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Numerics;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

internal class MoveSetCompoundState : ServerPacket
{
    public ObjectGuid MoverGUID;
    public List<MoveStateChange> StateChanges = new();
    public MoveSetCompoundState() : base(ServerOpcodes.MoveSetCompoundState, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(MoverGUID);
        WorldPacket.WriteInt32(StateChanges.Count);

        foreach (var stateChange in StateChanges)
            stateChange.Write(WorldPacket);
    }

    public struct CollisionHeightInfo
    {
        public float Height;
        public UpdateCollisionHeightReason Reason;
        public float Scale;
    }

    public struct KnockBackInfo
    {
        public Vector2 Direction;
        public float HorzSpeed;
        public float InitVertSpeed;
    }

    public class MoveStateChange
    {
        public CollisionHeightInfo? CollisionHeight;
        public KnockBackInfo? KnockBack;
        public ServerOpcodes MessageID;
        public MovementForce MovementForce;
        public ObjectGuid? MovementForceGUID;
        public int? MovementInertiaID;
        public uint? MovementInertiaLifetimeMs;
        public uint SequenceIndex;
        public float? Speed;
        public SpeedRange SpeedRange;
        public int? VehicleRecID;
        public MoveStateChange(ServerOpcodes messageId, uint sequenceIndex)
        {
            MessageID = messageId;
            SequenceIndex = sequenceIndex;
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt16((ushort)MessageID);
            data.WriteUInt32(SequenceIndex);
            data.WriteBit(Speed.HasValue);
            data.WriteBit(SpeedRange != null);
            data.WriteBit(KnockBack.HasValue);
            data.WriteBit(VehicleRecID.HasValue);
            data.WriteBit(CollisionHeight.HasValue);
            data.WriteBit(@MovementForce != null);
            data.WriteBit(MovementForceGUID.HasValue);
            data.WriteBit(MovementInertiaID.HasValue);
            data.WriteBit(MovementInertiaLifetimeMs.HasValue);
            data.FlushBits();

            @MovementForce?.Write(data);

            if (Speed.HasValue)
                data.WriteFloat(Speed.Value);

            if (SpeedRange != null)
            {
                data.WriteFloat(SpeedRange.Min);
                data.WriteFloat(SpeedRange.Max);
            }

            if (KnockBack.HasValue)
            {
                data.WriteFloat(KnockBack.Value.HorzSpeed);
                data.WriteVector2(KnockBack.Value.Direction);
                data.WriteFloat(KnockBack.Value.InitVertSpeed);
            }

            if (VehicleRecID.HasValue)
                data.WriteInt32(VehicleRecID.Value);

            if (CollisionHeight.HasValue)
            {
                data.WriteFloat(CollisionHeight.Value.Height);
                data.WriteFloat(CollisionHeight.Value.Scale);
                data.WriteBits(CollisionHeight.Value.Reason, 2);
                data.FlushBits();
            }

            if (MovementForceGUID.HasValue)
                data.WritePackedGuid(MovementForceGUID.Value);

            if (MovementInertiaID.HasValue)
                data.WriteInt32(MovementInertiaID.Value);

            if (MovementInertiaLifetimeMs.HasValue)
                data.WriteUInt32(MovementInertiaLifetimeMs.Value);
        }
    }

    public class SpeedRange
    {
        public float Max;
        public float Min;
    }
}