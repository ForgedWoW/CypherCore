﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Creatures;

namespace Game.Common.Entities.Creatures;

public class EquipmentInfo
{
	public EquipmentItem[] Items { get; set; } = new EquipmentItem[SharedConst.MaxEquipmentItems];
}
