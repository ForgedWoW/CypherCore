// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.DataStorage;

namespace Forged.RealmServer.Achievements;

public class Criteria
{
	public uint Id;
	public CriteriaRecord Entry;
	public ModifierTreeNode Modifier;
	public CriteriaFlagsCu FlagsCu;
}