// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

internal class MoveUpdateApplyMovementForce : ServerPacket
{
    public MovementForce Force = new();
    public MovementInfo Status = new();
    public MoveUpdateApplyMovementForce() : base(ServerOpcodes.MoveUpdateApplyMovementForce) { }

    public override void Write()
    {
        MovementExtensions.WriteMovementInfo(_worldPacket, Status);
        Force.Write(_worldPacket);
    }
}