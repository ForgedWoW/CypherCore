// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Common.Globals;
using Game.Common.Server;

namespace Game.Common.Networking.Packets.Query;

public class QuestPOIQueryResponse : ServerPacket
{
	public List<QuestPOIData> QuestPOIDataStats = new();
	public QuestPOIQueryResponse() : base(ServerOpcodes.QuestPoiQueryResponse) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(QuestPOIDataStats.Count);
		_worldPacket.WriteInt32(QuestPOIDataStats.Count);

		var useCache = WorldConfig.GetBoolValue(WorldCfg.CacheDataQueries);

		foreach (var questPOIData in QuestPOIDataStats)
			if (useCache)
				_worldPacket.WriteBytes(questPOIData.QueryDataBuffer);
			else
				questPOIData.Write(_worldPacket);
	}
}
