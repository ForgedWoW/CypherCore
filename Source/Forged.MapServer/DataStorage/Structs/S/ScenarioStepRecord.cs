// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class ScenarioStepRecord
{
    public uint CriteriaTreeId;
    public string Description;
    public ScenarioStepFlags Flags;
    public uint Id;
    public byte OrderIndex;
    public int RelatedStep;
    public uint RewardQuestID;

    public ushort ScenarioID;

    // Bonus step can only be completed if scenario is in the step specified in this field
    public ushort Supersedes;

    public string Title;

    // Used in conjunction with Proving Grounds scenarios, when sequencing steps (Not using step order?)
    public uint VisibilityPlayerConditionID;
    public ushort WidgetSetID;

    // helpers
    public bool IsBonusObjective()
    {
        return Flags.HasAnyFlag(ScenarioStepFlags.BonusObjective);
    }
}