// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Networking.Packets;

class WorldQuestUpdateResponse : ServerPacket
{
	readonly List<WorldQuestUpdateInfo> WorldQuestUpdates = new();
	public WorldQuestUpdateResponse() : base(ServerOpcodes.WorldQuestUpdateResponse, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(WorldQuestUpdates.Count);

		foreach (var worldQuestUpdate in WorldQuestUpdates)
		{
			_worldPacket.WriteInt64(worldQuestUpdate.LastUpdate);
			_worldPacket.WriteUInt32(worldQuestUpdate.QuestID);
			_worldPacket.WriteUInt32(worldQuestUpdate.Timer);
			_worldPacket.WriteInt32(worldQuestUpdate.VariableID);
			_worldPacket.WriteInt32(worldQuestUpdate.Value);
		}
	}
}