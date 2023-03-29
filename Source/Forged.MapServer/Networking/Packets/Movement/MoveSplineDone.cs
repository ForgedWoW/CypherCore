// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Movement;

internal class MoveSplineDone : ClientPacket
{
    public MovementInfo Status;
    public int SplineID;
    public MoveSplineDone(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Status = MovementExtensions.ReadMovementInfo(_worldPacket);
        SplineID = _worldPacket.ReadInt32();
    }
}