// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Item;

namespace Forged.MapServer.Networking.Packets.Bank;

public class AutoBankItem : ClientPacket
{
    public byte Bag;
    public InvUpdate Inv;
    public byte Slot;

    public AutoBankItem(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Inv = new InvUpdate(_worldPacket);
        Bag = _worldPacket.ReadUInt8();
        Slot = _worldPacket.ReadUInt8();
    }
}

// CMSG_BUY_REAGENT_BANK
// CMSG_REAGENT_BANK_DEPOSIT