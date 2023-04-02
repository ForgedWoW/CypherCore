// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Party;

internal class MinimapPing : ServerPacket
{
    public float PositionX;
    public float PositionY;
    public ObjectGuid Sender;
    public MinimapPing() : base(ServerOpcodes.MinimapPing) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Sender);
        WorldPacket.WriteFloat(PositionX);
        WorldPacket.WriteFloat(PositionY);
    }
}