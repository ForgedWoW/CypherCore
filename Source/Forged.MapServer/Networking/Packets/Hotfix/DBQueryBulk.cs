// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.Hotfix;

internal class DBQueryBulk : ClientPacket
{
    public List<DBQueryRecord> Queries = new();
    public uint TableHash;
    public DBQueryBulk(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        TableHash = _worldPacket.ReadUInt32();

        var count = _worldPacket.ReadBits<uint>(13);

        for (uint i = 0; i < count; ++i)
            Queries.Add(new DBQueryRecord(_worldPacket.ReadUInt32()));
    }

    public struct DBQueryRecord
    {
        public uint RecordID;

        public DBQueryRecord(uint recordId)
        {
            RecordID = recordId;
        }
    }
}