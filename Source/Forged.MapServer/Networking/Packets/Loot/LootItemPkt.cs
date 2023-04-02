// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.Loot;

internal class LootItemPkt : ClientPacket
{
    public List<LootRequest> Loot = new();
    public LootItemPkt(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        var Count = WorldPacket.ReadUInt32();

        for (uint i = 0; i < Count; ++i)
        {
            var loot = new LootRequest()
            {
                Object = WorldPacket.ReadPackedGuid(),
                LootListID = WorldPacket.ReadUInt8()
            };

            Loot.Add(loot);
        }
    }
}