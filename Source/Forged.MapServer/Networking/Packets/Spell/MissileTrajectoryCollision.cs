// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Spell;

internal class MissileTrajectoryCollision : ClientPacket
{
    public ObjectGuid CastID;
    public Vector3 CollisionPos;
    public uint SpellID;
    public ObjectGuid Target;
    public MissileTrajectoryCollision(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Target = WorldPacket.ReadPackedGuid();
        SpellID = WorldPacket.ReadUInt32();
        CastID = WorldPacket.ReadPackedGuid();
        CollisionPos = WorldPacket.ReadVector3();
    }
}