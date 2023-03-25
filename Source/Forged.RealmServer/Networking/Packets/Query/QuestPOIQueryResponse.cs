// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

public class QuestPOIQueryResponse : ServerPacket
{
	public List<QuestPOIData> QuestPOIDataStats = new();
	public QuestPOIQueryResponse() : base(ServerOpcodes.QuestPoiQueryResponse) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(QuestPOIDataStats.Count);
		_worldPacket.WriteInt32(QuestPOIDataStats.Count);

		var useCache = _worldConfig.GetBoolValue(WorldCfg.CacheDataQueries);

		foreach (var questPOIData in QuestPOIDataStats)
			if (useCache)
				_worldPacket.WriteBytes(questPOIData.QueryDataBuffer);
			else
				questPOIData.Write(_worldPacket);
	}
}