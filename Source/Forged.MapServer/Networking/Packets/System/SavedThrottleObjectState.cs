// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.System;

public struct SavedThrottleObjectState
{
    public uint LastResetTimeBeforeNow;
    public uint MaxTries;
    public uint PerMilliseconds;
    public uint TryCount;

    public void Write(WorldPacket data)
    {
        data.WriteUInt32(MaxTries);
        data.WriteUInt32(PerMilliseconds);
        data.WriteUInt32(TryCount);
        data.WriteUInt32(LastResetTimeBeforeNow);
    }
}