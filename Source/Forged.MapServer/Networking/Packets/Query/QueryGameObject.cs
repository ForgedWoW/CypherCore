﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Query;

public class QueryGameObject : ClientPacket
{
    public uint GameObjectID;
    public ObjectGuid Guid;
    public QueryGameObject(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        GameObjectID = _worldPacket.ReadUInt32();
        Guid = _worldPacket.ReadPackedGuid();
    }
}