// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

public class MoveUpdateSpeed : ServerPacket
{
    public float Speed = 1.0f;
    public MovementInfo Status;
    public MoveUpdateSpeed(ServerOpcodes opcode) : base(opcode, ConnectionType.Instance) { }

    public override void Write()
    {
        MovementExtensions.WriteMovementInfo(WorldPacket, Status);
        WorldPacket.WriteFloat(Speed);
    }
}