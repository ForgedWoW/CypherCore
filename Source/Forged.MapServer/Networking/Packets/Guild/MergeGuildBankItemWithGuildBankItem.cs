// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Guild;

internal class MergeGuildBankItemWithGuildBankItem : ClientPacket
{
    public ObjectGuid Banker;
    public byte BankSlot;
    public byte BankSlot1;
    public byte BankTab;
    public byte BankTab1;
    public uint StackCount;
    public MergeGuildBankItemWithGuildBankItem(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Banker = WorldPacket.ReadPackedGuid();
        BankTab = WorldPacket.ReadUInt8();
        BankSlot = WorldPacket.ReadUInt8();
        BankTab1 = WorldPacket.ReadUInt8();
        BankSlot1 = WorldPacket.ReadUInt8();
        StackCount = WorldPacket.ReadUInt32();
    }
}