// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Quest;

internal class QuestGiverStatusTrackedQuery : ClientPacket
{
    public List<ObjectGuid> QuestGiverGUIDs = new();

    public QuestGiverStatusTrackedQuery(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        var guidCount = _worldPacket.ReadUInt32();

        for (uint i = 0; i < guidCount; ++i)
            QuestGiverGUIDs.Add(_worldPacket.ReadPackedGuid());
    }
}