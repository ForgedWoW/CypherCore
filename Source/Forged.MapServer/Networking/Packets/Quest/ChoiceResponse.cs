// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

class ChoiceResponse : ClientPacket
{
	public int ChoiceID;
	public int ResponseIdentifier;
	public bool IsReroll;
	public ChoiceResponse(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ChoiceID = _worldPacket.ReadInt32();
		ResponseIdentifier = _worldPacket.ReadInt32();
		IsReroll = _worldPacket.HasBit();
	}
}