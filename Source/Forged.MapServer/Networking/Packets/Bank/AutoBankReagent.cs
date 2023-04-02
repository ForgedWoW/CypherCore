// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Item;

namespace Forged.MapServer.Networking.Packets.Bank;

internal class AutoBankReagent : ClientPacket
{
    public InvUpdate Inv;
    public byte PackSlot;
    public byte Slot;
    public AutoBankReagent(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Inv = new InvUpdate(WorldPacket);
        PackSlot = WorldPacket.ReadUInt8();
        Slot = WorldPacket.ReadUInt8();
    }
}