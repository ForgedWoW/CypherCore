// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Chat;

public class CTextEmote : ClientPacket
{
    public ObjectGuid Target;
    public int EmoteID;
    public int SoundIndex;
    public uint[] SpellVisualKitIDs;
    public int SequenceVariation;
    public CTextEmote(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Target = _worldPacket.ReadPackedGuid();
        EmoteID = _worldPacket.ReadInt32();
        SoundIndex = _worldPacket.ReadInt32();

        SpellVisualKitIDs = new uint[_worldPacket.ReadUInt32()];
        SequenceVariation = _worldPacket.ReadInt32();

        for (var i = 0; i < SpellVisualKitIDs.Length; ++i)
            SpellVisualKitIDs[i] = _worldPacket.ReadUInt32();
    }
}