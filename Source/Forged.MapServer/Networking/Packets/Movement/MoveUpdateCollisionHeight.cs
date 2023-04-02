// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

public class MoveUpdateCollisionHeight : ServerPacket
{
    public float Height = 1.0f;
    public float Scale = 1.0f;
    public MovementInfo Status;
    public MoveUpdateCollisionHeight() : base(ServerOpcodes.MoveUpdateCollisionHeight) { }

    public override void Write()
    {
        MovementExtensions.WriteMovementInfo(WorldPacket, Status);
        WorldPacket.WriteFloat(Height);
        WorldPacket.WriteFloat(Scale);
    }
}