// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Loot;

internal class LootMoneyNotify : ServerPacket
{
    public ulong Money;
    public ulong MoneyMod;
    public bool SoleLooter;
    public LootMoneyNotify() : base(ServerOpcodes.LootMoneyNotify) { }

    public override void Write()
    {
        WorldPacket.WriteUInt64(Money);
        WorldPacket.WriteUInt64(MoneyMod);
        WorldPacket.WriteBit(SoleLooter);
        WorldPacket.FlushBits();
    }
}