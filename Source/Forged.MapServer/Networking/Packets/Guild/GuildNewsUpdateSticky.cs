// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildNewsUpdateSticky : ClientPacket
{
    public ObjectGuid GuildGUID;
    public int NewsID;
    public bool Sticky;
    public GuildNewsUpdateSticky(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        GuildGUID = _worldPacket.ReadPackedGuid();
        NewsID = _worldPacket.ReadInt32();

        Sticky = _worldPacket.HasBit();
    }
}