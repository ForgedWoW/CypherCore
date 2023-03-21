// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class MasterLootCandidateList : ServerPacket
{
	public List<ObjectGuid> Players = new();
	public ObjectGuid LootObj;
	public MasterLootCandidateList() : base(ServerOpcodes.MasterLootCandidateList, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(LootObj);
		_worldPacket.WriteInt32(Players.Count);
		Players.ForEach(guid => _worldPacket.WritePackedGuid(guid));
	}
}