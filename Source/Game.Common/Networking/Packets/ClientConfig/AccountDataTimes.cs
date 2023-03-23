// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.ClientConfig;

public class AccountDataTimes : ServerPacket
{
	public ObjectGuid PlayerGuid;
	public long ServerTime;
	public long[] AccountTimes = new long[(int)AccountDataTypes.Max];
	public AccountDataTimes() : base(ServerOpcodes.AccountDataTimes) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(PlayerGuid);
		_worldPacket.WriteInt64(ServerTime);

		foreach (var accounttime in AccountTimes)
			_worldPacket.WriteInt64(accounttime);
	}
}
