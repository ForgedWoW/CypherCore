// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

internal class SetMovementAnimKit : ServerPacket
{
    public ushort AnimKitID;
    public ObjectGuid Unit;
    public SetMovementAnimKit() : base(ServerOpcodes.SetMovementAnimKit, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Unit);
        WorldPacket.WriteUInt16(AnimKitID);
    }
}