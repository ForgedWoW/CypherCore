// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Instance;

public struct InstanceLockPkt
{
    public uint CompletedMask;

    public uint DifficultyID;

    public bool Extended;

    public ulong InstanceID;

    public bool Locked;

    public uint MapID;

    public int TimeRemaining;

    public void Write(WorldPacket data)
    {
        data.WriteUInt32(MapID);
        data.WriteUInt32(DifficultyID);
        data.WriteUInt64(InstanceID);
        data.WriteInt32(TimeRemaining);
        data.WriteUInt32(CompletedMask);

        data.WriteBit(Locked);
        data.WriteBit(Extended);
        data.FlushBits();
    }
}