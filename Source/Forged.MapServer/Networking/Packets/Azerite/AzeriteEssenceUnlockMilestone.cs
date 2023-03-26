// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Azerite;

internal class AzeriteEssenceUnlockMilestone : ClientPacket
{
	public int AzeriteItemMilestonePowerID;
	public AzeriteEssenceUnlockMilestone(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		AzeriteItemMilestonePowerID = _worldPacket.ReadInt32();
	}
}