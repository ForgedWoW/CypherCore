// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Item;

namespace Forged.MapServer.Networking.Packets.Bank;

public class AutoStoreBankItem : ClientPacket
{
    public byte Bag;
    public InvUpdate Inv;
    public byte Slot;

    public AutoStoreBankItem(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Inv = new InvUpdate(WorldPacket);
        Bag = WorldPacket.ReadUInt8();
        Slot = WorldPacket.ReadUInt8();
    }
}