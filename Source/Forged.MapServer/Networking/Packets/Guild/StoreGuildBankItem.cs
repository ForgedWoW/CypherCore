// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Guild;

internal class StoreGuildBankItem : ClientPacket
{
    public ObjectGuid Banker;
    public byte BankSlot;
    public byte BankTab;
    public byte ContainerItemSlot;
    public byte? ContainerSlot;
    public StoreGuildBankItem(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Banker = WorldPacket.ReadPackedGuid();
        BankTab = WorldPacket.ReadUInt8();
        BankSlot = WorldPacket.ReadUInt8();
        ;
        ContainerItemSlot = WorldPacket.ReadUInt8();

        if (WorldPacket.HasBit())
            ContainerSlot = WorldPacket.ReadUInt8();
    }
}