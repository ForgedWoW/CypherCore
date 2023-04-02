// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

internal class MoveUpdateRemoveMovementForce : ServerPacket
{
    public MovementInfo Status = new();
    public ObjectGuid TriggerGUID;
    public MoveUpdateRemoveMovementForce() : base(ServerOpcodes.MoveUpdateRemoveMovementForce) { }

    public override void Write()
    {
        MovementExtensions.WriteMovementInfo(WorldPacket, Status);
        WorldPacket.WritePackedGuid(TriggerGUID);
    }
}