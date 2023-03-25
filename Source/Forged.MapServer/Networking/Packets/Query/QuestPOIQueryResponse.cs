// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Globals;
using Forged.MapServer.Server;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Query;

public class QuestPOIQueryResponse : ServerPacket
{
	public List<QuestPOIData> QuestPOIDataStats = new();
	public QuestPOIQueryResponse() : base(ServerOpcodes.QuestPoiQueryResponse) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(QuestPOIDataStats.Count);
		_worldPacket.WriteInt32(QuestPOIDataStats.Count);

		var useCache = GetDefaultValue("CacheDataQueries", true);

		foreach (var questPOIData in QuestPOIDataStats)
			if (useCache)
				_worldPacket.WriteBytes(questPOIData.QueryDataBuffer);
			else
				questPOIData.Write(_worldPacket);
	}
}