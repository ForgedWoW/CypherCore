// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Movement;

namespace Forged.MapServer.Networking.Packets.Spell;

internal class UpdateMissileTrajectory : ClientPacket
{
    public ObjectGuid CastID;
    public Vector3 FirePos;
    public ObjectGuid Guid;
    public Vector3 ImpactPos;
    public ushort MoveMsgID;
    public float Pitch;
    public float Speed;
    public uint SpellID;
    public MovementInfo Status;
    public UpdateMissileTrajectory(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Guid = WorldPacket.ReadPackedGuid();
        CastID = WorldPacket.ReadPackedGuid();
        MoveMsgID = WorldPacket.ReadUInt16();
        SpellID = WorldPacket.ReadUInt32();
        Pitch = WorldPacket.ReadFloat();
        Speed = WorldPacket.ReadFloat();
        FirePos = WorldPacket.ReadVector3();
        ImpactPos = WorldPacket.ReadVector3();
        var hasStatus = WorldPacket.HasBit();

        WorldPacket.ResetBitPos();

        if (hasStatus)
            Status = MovementExtensions.ReadMovementInfo(WorldPacket);
    }
}