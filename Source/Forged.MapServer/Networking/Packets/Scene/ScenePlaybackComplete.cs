// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Scene;

class ScenePlaybackComplete : ClientPacket
{
	public uint SceneInstanceID;
	public ScenePlaybackComplete(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		SceneInstanceID = _worldPacket.ReadUInt32();
	}
}