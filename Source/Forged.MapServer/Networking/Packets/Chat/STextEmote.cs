// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Chat;

public class STextEmote : ServerPacket
{
    public int EmoteID;
    public int SoundIndex = -1;
    public ObjectGuid SourceAccountGUID;
    public ObjectGuid SourceGUID;
    public ObjectGuid TargetGUID;
    public STextEmote() : base(ServerOpcodes.TextEmote, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(SourceGUID);
        WorldPacket.WritePackedGuid(SourceAccountGUID);
        WorldPacket.WriteInt32(EmoteID);
        WorldPacket.WriteInt32(SoundIndex);
        WorldPacket.WritePackedGuid(TargetGUID);
    }
}