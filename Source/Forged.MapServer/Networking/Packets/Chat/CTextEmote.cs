// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Chat;

public class CTextEmote : ClientPacket
{
    public int EmoteID;
    public int SequenceVariation;
    public int SoundIndex;
    public uint[] SpellVisualKitIDs;
    public ObjectGuid Target;
    public CTextEmote(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Target = WorldPacket.ReadPackedGuid();
        EmoteID = WorldPacket.ReadInt32();
        SoundIndex = WorldPacket.ReadInt32();

        SpellVisualKitIDs = new uint[WorldPacket.ReadUInt32()];
        SequenceVariation = WorldPacket.ReadInt32();

        for (var i = 0; i < SpellVisualKitIDs.Length; ++i)
            SpellVisualKitIDs[i] = WorldPacket.ReadUInt32();
    }
}