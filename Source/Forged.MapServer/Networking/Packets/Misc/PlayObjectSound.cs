// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

internal class PlayObjectSound : ServerPacket
{
    public ObjectGuid TargetObjectGUID;
    public ObjectGuid SourceObjectGUID;
    public uint SoundKitID;
    public Vector3 Position;
    public int BroadcastTextID;
    public PlayObjectSound() : base(ServerOpcodes.PlayObjectSound) { }

    public override void Write()
    {
        _worldPacket.WriteUInt32(SoundKitID);
        _worldPacket.WritePackedGuid(SourceObjectGUID);
        _worldPacket.WritePackedGuid(TargetObjectGUID);
        _worldPacket.WriteVector3(Position);
        _worldPacket.WriteInt32(BroadcastTextID);
    }
}