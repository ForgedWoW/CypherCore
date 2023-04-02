// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Networking.Packets.Spell;

namespace Forged.MapServer.Networking.Packets.Movement;

public class MonsterSplineUnknown901
{
    public Array<Inner> Data = new(16);

    public void Write(WorldPacket data)
    {
        foreach (var unkInner in Data)
        {
            data.WriteInt32(unkInner.Unknown_1);
            unkInner.Visual.Write(data);
            data.WriteUInt32(unkInner.Unknown_4);
        }
    }

    public struct Inner
    {
        public int Unknown_1;
        public uint Unknown_4;
        public SpellCastVisual Visual;
    }
}