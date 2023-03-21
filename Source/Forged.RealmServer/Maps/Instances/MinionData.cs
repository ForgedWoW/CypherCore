// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Maps;

public class MinionData
{
	public uint Entry { get; set; }
	public uint BossId { get; set; }

	public MinionData(uint _entry, uint _bossid)
	{
		Entry = _entry;
		BossId = _bossid;
	}
}