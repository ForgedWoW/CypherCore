// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Chat;

public class EmoteMessage : ServerPacket
{
    public uint EmoteID;
    public ObjectGuid Guid;
    public int SequenceVariation;
    public List<uint> SpellVisualKitIDs = new();
    public EmoteMessage() : base(ServerOpcodes.Emote, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Guid);
        WorldPacket.WriteUInt32(EmoteID);
        WorldPacket.WriteInt32(SpellVisualKitIDs.Count);
        WorldPacket.WriteInt32(SequenceVariation);

        foreach (var id in SpellVisualKitIDs)
            WorldPacket.WriteUInt32(id);
    }
}