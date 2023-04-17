// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.Garrison;

internal class GarrisonEventList
{
    public List<GarrisonEventEntry> Events = new();
    public int Type;

    public void Write(WorldPacket data)
    {
        data.WriteInt32(Type);
        data.WriteInt32(Events.Count);

        foreach (var eventEntry in Events)
            eventEntry.Write(data);
    }
}