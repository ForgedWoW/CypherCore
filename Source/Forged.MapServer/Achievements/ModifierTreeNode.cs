// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.Structs.M;

namespace Forged.MapServer.Achievements;

public class ModifierTreeNode
{
    public List<ModifierTreeNode> Children = new();
    public ModifierTreeRecord Entry;
}