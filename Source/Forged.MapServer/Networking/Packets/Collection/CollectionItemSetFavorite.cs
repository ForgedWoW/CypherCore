// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Collection;

internal class CollectionItemSetFavorite : ClientPacket
{
    public uint Id;
    public bool IsFavorite;
    public CollectionType Type;
    public CollectionItemSetFavorite(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Type = (CollectionType)WorldPacket.ReadUInt32();
        Id = WorldPacket.ReadUInt32();
        IsFavorite = WorldPacket.HasBit();
    }
}