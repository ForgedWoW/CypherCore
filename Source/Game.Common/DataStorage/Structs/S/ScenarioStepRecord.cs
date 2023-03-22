// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Game.DataStorage;

public sealed class ScenarioStepRecord
{
	public uint Id;
	public string Description;
	public string Title;
	public ushort ScenarioID;
	public uint CriteriaTreeId;
	public uint RewardQuestID;
	public int RelatedStep;   // Bonus step can only be completed if scenario is in the step specified in this field
	public ushort Supersedes; // Used in conjunction with Proving Grounds scenarios, when sequencing steps (Not using step order?)
	public byte OrderIndex;
	public ScenarioStepFlags Flags;
	public uint VisibilityPlayerConditionID;
	public ushort WidgetSetID;

	// helpers
	public bool IsBonusObjective()
	{
		return Flags.HasAnyFlag(ScenarioStepFlags.BonusObjective);
	}
}