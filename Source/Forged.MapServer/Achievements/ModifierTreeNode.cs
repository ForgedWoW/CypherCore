﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.DataStorage;

namespace Game.Achievements;

public class ModifierTreeNode
{
	public ModifierTreeRecord Entry;
	public List<ModifierTreeNode> Children = new();
}