// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking;

namespace Forged.MapServer.DataStorage;

public class HotfixRecord
{
    public enum Status
    {
        NotSet = 0,
        Valid = 1,
        RecordRemoved = 2,
        Invalid = 3,
        NotPublic = 4
    }

    public uint TableHash;
    public int RecordID;
    public HotfixId ID;
    public Status HotfixStatus = Status.Invalid;

    public void Write(WorldPacket data)
    {
        ID.Write(data);
        data.WriteUInt32(TableHash);
        data.WriteInt32(RecordID);
    }

    public void Read(WorldPacket data)
    {
        ID.Read(data);
        TableHash = data.ReadUInt32();
        RecordID = data.ReadInt32();
    }
}