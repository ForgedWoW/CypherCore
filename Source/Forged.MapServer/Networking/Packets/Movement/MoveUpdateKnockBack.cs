// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

public class MoveUpdateKnockBack : ServerPacket
{
    public MovementInfo Status;
    public MoveUpdateKnockBack() : base(ServerOpcodes.MoveUpdateKnockBack) { }

    public override void Write()
    {
        MovementExtensions.WriteMovementInfo(WorldPacket, Status);
    }
}