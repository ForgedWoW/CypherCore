// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildBankQueryTab : ClientPacket
{
    public ObjectGuid Banker;
    public bool FullUpdate;
    public byte Tab;
    public GuildBankQueryTab(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Banker = WorldPacket.ReadPackedGuid();
        Tab = WorldPacket.ReadUInt8();

        FullUpdate = WorldPacket.HasBit();
    }
}