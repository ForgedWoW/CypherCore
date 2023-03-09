// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class GuildEventBankMoneyChanged : ServerPacket
{
	public ulong Money;
	public GuildEventBankMoneyChanged() : base(ServerOpcodes.GuildEventBankMoneyChanged) { }

	public override void Write()
	{
		_worldPacket.WriteUInt64(Money);
	}
}