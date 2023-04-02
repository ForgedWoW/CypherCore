// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Globals;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Query;

public class QuestPOIQueryResponse : ServerPacket
{
    public List<QuestPOIData> QuestPOIDataStats = new();
    public QuestPOIQueryResponse() : base(ServerOpcodes.QuestPoiQueryResponse) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(QuestPOIDataStats.Count);
        WorldPacket.WriteInt32(QuestPOIDataStats.Count);

        var useCache = GetDefaultValue("CacheDataQueries", true);

        foreach (var questPOIData in QuestPOIDataStats)
            if (useCache)
                WorldPacket.WriteBytes(questPOIData.QueryDataBuffer);
            else
                questPOIData.Write(WorldPacket);
    }
}