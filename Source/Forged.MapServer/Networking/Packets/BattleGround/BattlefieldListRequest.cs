// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

class BattlefieldListRequest : ClientPacket
{
	public int ListID;
	public BattlefieldListRequest(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ListID = _worldPacket.ReadInt32();
	}
}