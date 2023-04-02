// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Query;

public class QueryPageText : ClientPacket
{
    public ObjectGuid ItemGUID;
    public uint PageTextID;
    public QueryPageText(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PageTextID = WorldPacket.ReadUInt32();
        ItemGUID = WorldPacket.ReadPackedGuid();
    }
}