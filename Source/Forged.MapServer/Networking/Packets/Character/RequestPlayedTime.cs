// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Character;

public class RequestPlayedTime : ClientPacket
{
	public bool TriggerScriptEvent;
	public RequestPlayedTime(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		TriggerScriptEvent = _worldPacket.HasBit();
	}
}