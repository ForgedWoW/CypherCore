// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Quest;

internal struct PlayerChoiceResponseMawPower
{
    public int MaxStacks;
    public int? Rarity;
    public uint? RarityColor;
    public int SpellID;
    public int TypeArtFileID;
    public int Unused901_1;
    public int Unused901_2;
    public void Write(WorldPacket data)
    {
        data.WriteInt32(Unused901_1);
        data.WriteInt32(TypeArtFileID);
        data.WriteInt32(Unused901_2);
        data.WriteInt32(SpellID);
        data.WriteInt32(MaxStacks);
        data.WriteBit(Rarity.HasValue);
        data.WriteBit(RarityColor.HasValue);
        data.FlushBits();

        if (Rarity.HasValue)
            data.WriteInt32(Rarity.Value);

        if (RarityColor.HasValue)
            data.WriteUInt32(RarityColor.Value);
    }
}