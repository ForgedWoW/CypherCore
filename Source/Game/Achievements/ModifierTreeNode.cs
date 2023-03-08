using System.Collections.Generic;
using Game.DataStorage;

namespace Game.Achievements;

public class ModifierTreeNode
{
	public ModifierTreeRecord Entry;
	public List<ModifierTreeNode> Children = new();
}