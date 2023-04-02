// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

internal class PlaySpeakerBoxSound : ServerPacket
{
    public uint SoundKitID;
    public ObjectGuid SourceObjectGUID;
    public PlaySpeakerBoxSound(ObjectGuid sourceObjectGuid, uint soundKitID) : base(ServerOpcodes.PlaySpeakerbotSound)
    {
        SourceObjectGUID = sourceObjectGuid;
        SoundKitID = soundKitID;
    }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(SourceObjectGUID);
        WorldPacket.WriteUInt32(SoundKitID);
    }
}