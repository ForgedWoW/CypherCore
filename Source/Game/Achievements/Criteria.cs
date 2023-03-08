using Framework.Constants;
using Game.DataStorage;

namespace Game.Achievements;

public class Criteria
{
	public uint Id;
	public CriteriaRecord Entry;
	public ModifierTreeNode Modifier;
	public CriteriaFlagsCu FlagsCu;
}