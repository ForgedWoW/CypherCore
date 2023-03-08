// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

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

class CancelScene : ServerPacket
{
	public uint SceneInstanceID;
	public CancelScene() : base(ServerOpcodes.CancelScene, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(SceneInstanceID);
	}
}

class SceneTriggerEvent : ClientPacket
{
	public uint SceneInstanceID;
	public string _Event;
	public SceneTriggerEvent(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var len = _worldPacket.ReadBits<uint>(6);
		SceneInstanceID = _worldPacket.ReadUInt32();
		_Event = _worldPacket.ReadString(len);
	}
}

class ScenePlaybackComplete : ClientPacket
{
	public uint SceneInstanceID;
	public ScenePlaybackComplete(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		SceneInstanceID = _worldPacket.ReadUInt32();
	}
}

class ScenePlaybackCanceled : ClientPacket
{
	public uint SceneInstanceID;
	public ScenePlaybackCanceled(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		SceneInstanceID = _worldPacket.ReadUInt32();
	}
}