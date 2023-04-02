// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildBankUpdateTab : ClientPacket
{
    public ObjectGuid Banker;
    public byte BankTab;
    public string Icon;
    public string Name;
    public GuildBankUpdateTab(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Banker = WorldPacket.ReadPackedGuid();
        BankTab = WorldPacket.ReadUInt8();

        WorldPacket.ResetBitPos();
        var nameLen = WorldPacket.ReadBits<uint>(7);
        var iconLen = WorldPacket.ReadBits<uint>(9);

        Name = WorldPacket.ReadString(nameLen);
        Icon = WorldPacket.ReadString(iconLen);
    }
}