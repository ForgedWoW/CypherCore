// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

public class MoveUpdate : ServerPacket
{
    public MovementInfo Status;
    public MoveUpdate() : base(ServerOpcodes.MoveUpdate, ConnectionType.Instance) { }

    public override void Write()
    {
        MovementExtensions.WriteMovementInfo(_worldPacket, Status);
    }
}