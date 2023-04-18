// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Movement;

internal class MoveRemoveMovementForceAck : ClientPacket
{
    public MovementAck Ack;
    public ObjectGuid ID;
    public MoveRemoveMovementForceAck(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Ack.Read(WorldPacket);
        ID = WorldPacket.ReadPackedGuid();
    }
}