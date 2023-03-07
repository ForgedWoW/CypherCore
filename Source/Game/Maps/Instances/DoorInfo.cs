// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Maps;

class DoorInfo
{
	public BossInfo BossInfo { get; set; }
    public DoorType Type { get; set; }

    public DoorInfo(BossInfo bossInfo, DoorType doorType)
	{
		BossInfo = bossInfo;
		Type     = doorType;
	}
}