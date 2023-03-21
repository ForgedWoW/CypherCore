// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class SetPlayerDeclinedNamesResult : ServerPacket
{
	public ObjectGuid Player;
	public DeclinedNameResult ResultCode;
	public SetPlayerDeclinedNamesResult() : base(ServerOpcodes.SetPlayerDeclinedNamesResult) { }

	public override void Write()
	{
		_worldPacket.WriteInt32((int)ResultCode);
		_worldPacket.WritePackedGuid(Player);
	}
}