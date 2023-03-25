// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Quest;

public class UiMapQuestLinesRequest : ClientPacket
{
	public int UiMapID;

	public UiMapQuestLinesRequest(WorldPacket worldPacket) : base(worldPacket) { }

	public override void Read()
	{
		UiMapID = _worldPacket.ReadInt32();
	}
}