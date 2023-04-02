// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Movement;

public struct MovementAck
{
    public int AckIndex;

    public MovementInfo Status;

    public void Read(WorldPacket data)
    {
        Status = MovementExtensions.ReadMovementInfo(data);
        AckIndex = data.ReadInt32();
    }
}