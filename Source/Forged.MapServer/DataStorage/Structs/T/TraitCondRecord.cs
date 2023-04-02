// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed class TraitCondRecord
{
    public uint AchievementID;
    public int CondType;
    public int Flags;
    public int FreeSharedStringID;
    public int GrantedRanks;
    public uint Id;
    public uint QuestID;
    public int RequiredLevel;
    public int SpecSetID;
    public int SpendMoreSharedStringID;
    public int SpentAmountRequired;
    public int TraitCurrencyID;
    public int TraitNodeGroupID;
    public int TraitNodeID;
    public int TraitTreeID;
    public TraitConditionType GetCondType()
    {
        return (TraitConditionType)CondType;
    }
}