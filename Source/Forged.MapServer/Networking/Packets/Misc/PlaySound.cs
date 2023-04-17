// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

internal class PlaySound : ServerPacket
{
    public uint BroadcastTextID;
    public uint SoundKitID;
    public ObjectGuid SourceObjectGuid;

    public PlaySound(ObjectGuid sourceObjectGuid, uint soundKitID, uint broadcastTextId) : base(ServerOpcodes.PlaySound)
    {
        SourceObjectGuid = sourceObjectGuid;
        SoundKitID = soundKitID;
        BroadcastTextID = broadcastTextId;
    }

    public override void Write()
    {
        WorldPacket.WriteUInt32(SoundKitID);
        WorldPacket.WritePackedGuid(SourceObjectGuid);
        WorldPacket.WriteUInt32(BroadcastTextID);
    }
}