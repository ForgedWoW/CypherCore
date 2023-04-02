// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

public class NewWorld : ServerPacket
{
    public TeleportLocation Loc = new();
    public uint MapID;
    public Position MovementOffset;
    public uint Reason;
    // Adjusts all pending movement events by this offset
    public NewWorld() : base(ServerOpcodes.NewWorld) { }

    public override void Write()
    {
        _worldPacket.WriteUInt32(MapID);
        Loc.Write(_worldPacket);
        _worldPacket.WriteUInt32(Reason);
        _worldPacket.WriteXYZ(MovementOffset);
    }
}