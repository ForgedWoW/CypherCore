﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Common.Entities.Items;

public class RandomBonusListIds
{
	public List<uint> BonusListIDs { get; set; } = new();
	public List<double> Chances { get; set; } = new();
}
