// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Misc;

internal class MountSetFavorite : ClientPacket
{
    public bool IsFavorite;
    public uint MountSpellID;
    public MountSetFavorite(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        MountSpellID = WorldPacket.ReadUInt32();
        IsFavorite = WorldPacket.HasBit();
    }
}