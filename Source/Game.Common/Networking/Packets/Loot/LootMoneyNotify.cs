// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class LootMoneyNotify : ServerPacket
{
	public ulong Money;
	public ulong MoneyMod;
	public bool SoleLooter;
	public LootMoneyNotify() : base(ServerOpcodes.LootMoneyNotify) { }

	public override void Write()
	{
		_worldPacket.WriteUInt64(Money);
		_worldPacket.WriteUInt64(MoneyMod);
		_worldPacket.WriteBit(SoleLooter);
		_worldPacket.FlushBits();
	}
}