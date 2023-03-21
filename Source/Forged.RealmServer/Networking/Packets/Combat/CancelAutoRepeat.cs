// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class CancelAutoRepeat : ServerPacket
{
	public ObjectGuid Guid;
	public CancelAutoRepeat() : base(ServerOpcodes.CancelAutoRepeat) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Guid);
	}
}