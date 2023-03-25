// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Scene;

class PlayScene : ServerPacket
{
	public uint SceneID;
	public uint PlaybackFlags;
	public uint SceneInstanceID;
	public uint SceneScriptPackageID;
	public ObjectGuid TransportGUID;
	public Position Location;
	public bool Encrypted;
	public PlayScene() : base(ServerOpcodes.PlayScene, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(SceneID);
		_worldPacket.WriteUInt32(PlaybackFlags);
		_worldPacket.WriteUInt32(SceneInstanceID);
		_worldPacket.WriteUInt32(SceneScriptPackageID);
		_worldPacket.WritePackedGuid(TransportGUID);
		_worldPacket.WriteXYZO(Location);
		_worldPacket.WriteBit(Encrypted);
		_worldPacket.FlushBits();
	}
}