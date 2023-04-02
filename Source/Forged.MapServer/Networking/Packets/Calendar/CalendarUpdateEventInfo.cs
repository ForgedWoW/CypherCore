// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Calendar;

internal struct CalendarUpdateEventInfo
{
    public ulong ClubID;

    public string Description;

    public ulong EventID;

    public byte EventType;

    public uint Flags;

    public ulong ModeratorID;

    public uint TextureID;

    public long Time;

    public string Title;

    public void Read(WorldPacket data)
    {
        ClubID = data.ReadUInt64();
        EventID = data.ReadUInt64();
        ModeratorID = data.ReadUInt64();
        EventType = data.ReadUInt8();
        TextureID = data.ReadUInt32();
        Time = data.ReadPackedTime();
        Flags = data.ReadUInt32();

        var titleLen = data.ReadBits<byte>(8);
        var descLen = data.ReadBits<ushort>(11);

        Title = data.ReadString(titleLen);
        Description = data.ReadString(descLen);
    }
}