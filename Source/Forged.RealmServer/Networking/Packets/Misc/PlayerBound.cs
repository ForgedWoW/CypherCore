// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class PlayerBound : ServerPacket
{
	readonly uint AreaID;

	readonly ObjectGuid BinderID;

	public PlayerBound(ObjectGuid binderId, uint areaId) : base(ServerOpcodes.PlayerBound)
	{
		BinderID = binderId;
		AreaID = areaId;
	}

	public override void Write()
	{
		_worldPacket.WritePackedGuid(BinderID);
		_worldPacket.WriteUInt32(AreaID);
	}
}