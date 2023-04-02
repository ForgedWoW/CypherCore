// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Guild;

internal class SwapGuildBankItemWithGuildBankItem : ClientPacket
{
    public ObjectGuid Banker;
    public byte[] BankSlot = new byte[2];
    public byte[] BankTab = new byte[2];
    public SwapGuildBankItemWithGuildBankItem(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Banker = _worldPacket.ReadPackedGuid();
        BankTab[0] = _worldPacket.ReadUInt8();
        BankSlot[0] = _worldPacket.ReadUInt8();
        BankTab[1] = _worldPacket.ReadUInt8();
        BankSlot[1] = _worldPacket.ReadUInt8();
    }
}