// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Quest;

internal class WorldQuestUpdateResponse : ServerPacket
{
    private readonly List<WorldQuestUpdateInfo> WorldQuestUpdates = new();
    public WorldQuestUpdateResponse() : base(ServerOpcodes.WorldQuestUpdateResponse, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(WorldQuestUpdates.Count);

        foreach (var worldQuestUpdate in WorldQuestUpdates)
        {
            WorldPacket.WriteInt64(worldQuestUpdate.LastUpdate);
            WorldPacket.WriteUInt32(worldQuestUpdate.QuestID);
            WorldPacket.WriteUInt32(worldQuestUpdate.Timer);
            WorldPacket.WriteInt32(worldQuestUpdate.VariableID);
            WorldPacket.WriteInt32(worldQuestUpdate.Value);
        }
    }
}