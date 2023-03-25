// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public class GuildBankDepositMoney : ClientPacket
{
	public ObjectGuid Banker;
	public ulong Money;
	public GuildBankDepositMoney(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Banker = _worldPacket.ReadPackedGuid();
		Money = _worldPacket.ReadUInt64();
	}
}