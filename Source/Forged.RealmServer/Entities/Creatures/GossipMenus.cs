﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Conditions;

namespace Game.Misc;

public class GossipMenus
{
	public uint MenuId { get; set; }
	public uint TextId { get; set; }
	public List<Condition> Conditions { get; set; } = new();
}