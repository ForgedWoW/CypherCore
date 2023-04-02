// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Guild;

internal class AutoStoreGuildBankItem : ClientPacket
{
    public ObjectGuid Banker;
    public byte BankSlot;
    public byte BankTab;
    public AutoStoreGuildBankItem(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Banker = _worldPacket.ReadPackedGuid();
        BankTab = _worldPacket.ReadUInt8();
        BankSlot = _worldPacket.ReadUInt8();
    }
}