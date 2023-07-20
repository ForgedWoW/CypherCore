// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Globals;
using Framework.Constants;
using Framework.Util;
using Microsoft.Extensions.Configuration;

namespace Forged.MapServer.Networking.Packets.Query;

public class QuestPOIQueryResponse : ServerPacket
{
    private readonly IConfiguration _configuration;
    public List<QuestPOIData> QuestPOIDataStats = new();
    public QuestPOIQueryResponse(IConfiguration configuration) : base(ServerOpcodes.QuestPoiQueryResponse)
    {
        _configuration = configuration;
    }

    public override void Write()
    {
        WorldPacket.WriteInt32(QuestPOIDataStats.Count);
        WorldPacket.WriteInt32(QuestPOIDataStats.Count);

        var useCache = _configuration.GetDefaultValue("CacheDataQueries", true);

        foreach (var questPOIData in QuestPOIDataStats)
            if (useCache)
                WorldPacket.WriteBytes(questPOIData.QueryDataBuffer);
            else
                questPOIData.Write(WorldPacket);
    }
}