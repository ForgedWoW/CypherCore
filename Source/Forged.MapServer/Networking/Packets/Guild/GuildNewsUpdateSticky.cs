// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public class GuildNewsUpdateSticky : ClientPacket
{
	public int NewsID;
	public ObjectGuid GuildGUID;
	public bool Sticky;
	public GuildNewsUpdateSticky(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		GuildGUID = _worldPacket.ReadPackedGuid();
		NewsID = _worldPacket.ReadInt32();

		Sticky = _worldPacket.HasBit();
	}
}