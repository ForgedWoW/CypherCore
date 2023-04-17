// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.Movement;

public class MonsterSplineFilter
{
    public short AddedToStart;
    public float BaseSpeed;
    public float DistToPrevFilterKey;
    public byte FilterFlags;
    public List<MonsterSplineFilterKey> FilterKeys = new();
    public short StartOffset;

    public void Write(WorldPacket data)
    {
        data.WriteInt32(FilterKeys.Count);
        data.WriteFloat(BaseSpeed);
        data.WriteInt16(StartOffset);
        data.WriteFloat(DistToPrevFilterKey);
        data.WriteInt16(AddedToStart);

        FilterKeys.ForEach(p => p.Write(data));

        data.WriteBits(FilterFlags, 2);
        data.FlushBits();
    }
}