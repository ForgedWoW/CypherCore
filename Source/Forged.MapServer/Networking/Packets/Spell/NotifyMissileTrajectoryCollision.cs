// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

internal class NotifyMissileTrajectoryCollision : ServerPacket
{
    public ObjectGuid Caster;
    public ObjectGuid CastID;
    public Vector3 CollisionPos;
    public NotifyMissileTrajectoryCollision() : base(ServerOpcodes.NotifyMissileTrajectoryCollision) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Caster);
        WorldPacket.WritePackedGuid(CastID);
        WorldPacket.WriteVector3(CollisionPos);
    }
}