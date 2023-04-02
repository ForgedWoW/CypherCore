// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Scene;

internal class PlayScene : ServerPacket
{
    public bool Encrypted;
    public Position Location;
    public uint PlaybackFlags;
    public uint SceneID;
    public uint SceneInstanceID;
    public uint SceneScriptPackageID;
    public ObjectGuid TransportGUID;
    public PlayScene() : base(ServerOpcodes.PlayScene, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(SceneID);
        WorldPacket.WriteUInt32(PlaybackFlags);
        WorldPacket.WriteUInt32(SceneInstanceID);
        WorldPacket.WriteUInt32(SceneScriptPackageID);
        WorldPacket.WritePackedGuid(TransportGUID);
        WorldPacket.WriteXYZO(Location);
        WorldPacket.WriteBit(Encrypted);
        WorldPacket.FlushBits();
    }
}