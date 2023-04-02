// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.BattleGround;

public struct BattlegroundPlayerPosition
{
    public sbyte ArenaSlot;

    public ObjectGuid Guid;

    public sbyte IconID;

    public Vector2 Pos;

    public void Write(WorldPacket data)
    {
        data.WritePackedGuid(Guid);
        data.WriteVector2(Pos);
        data.WriteInt8(IconID);
        data.WriteInt8(ArenaSlot);
    }
}