// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed class TraitCondRecord
{
    public uint Id;
    public int CondType;
    public int TraitTreeID;
    public int GrantedRanks;
    public uint QuestID;
    public uint AchievementID;
    public int SpecSetID;
    public int TraitNodeGroupID;
    public int TraitNodeID;
    public int TraitCurrencyID;
    public int SpentAmountRequired;
    public int Flags;
    public int RequiredLevel;
    public int FreeSharedStringID;
    public int SpendMoreSharedStringID;

    public TraitConditionType GetCondType()
    {
        return (TraitConditionType)CondType;
    }
}