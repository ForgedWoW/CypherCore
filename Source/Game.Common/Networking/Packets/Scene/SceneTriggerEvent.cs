// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Scene;

public class SceneTriggerEvent : ClientPacket
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
