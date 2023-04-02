// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

internal class PlayObjectSound : ServerPacket
{
    public int BroadcastTextID;
    public Vector3 Position;
    public uint SoundKitID;
    public ObjectGuid SourceObjectGUID;
    public ObjectGuid TargetObjectGUID;
    public PlayObjectSound() : base(ServerOpcodes.PlayObjectSound) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(SoundKitID);
        WorldPacket.WritePackedGuid(SourceObjectGUID);
        WorldPacket.WritePackedGuid(TargetObjectGUID);
        WorldPacket.WriteVector3(Position);
        WorldPacket.WriteInt32(BroadcastTextID);
    }
}