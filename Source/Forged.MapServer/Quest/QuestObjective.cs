// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Quest;

public class QuestObjective
{
    public int Amount;
    public string Description;
    public QuestObjectiveFlags Flags;
    public uint Flags2;
    public uint Id;
    public int ObjectID;
    public float ProgressBarWeight;
    public uint QuestID;
    public sbyte StorageIndex;
    public QuestObjectiveType Type;
    public int[] VisualEffects = Array.Empty<int>();

    public static bool CanAlwaysBeProgressedInRaid(QuestObjectiveType type)
    {
        switch (type)
        {
            case QuestObjectiveType.Item:
            case QuestObjectiveType.Currency:
            case QuestObjectiveType.LearnSpell:
            case QuestObjectiveType.MinReputation:
            case QuestObjectiveType.MaxReputation:
            case QuestObjectiveType.Money:
            case QuestObjectiveType.HaveCurrency:
            case QuestObjectiveType.IncreaseReputation:
                return true;
            
        }

        return false;
    }

    public bool IsStoringFlag()
    {
        switch (Type)
        {
            case QuestObjectiveType.AreaTrigger:
            case QuestObjectiveType.WinPetBattleAgainstNpc:
            case QuestObjectiveType.DefeatBattlePet:
            case QuestObjectiveType.CriteriaTree:
            case QuestObjectiveType.AreaTriggerEnter:
            case QuestObjectiveType.AreaTriggerExit:
                return true;
            
        }

        return false;
    }

    public bool IsStoringValue()
    {
        switch (Type)
        {
            case QuestObjectiveType.Monster:
            case QuestObjectiveType.Item:
            case QuestObjectiveType.GameObject:
            case QuestObjectiveType.TalkTo:
            case QuestObjectiveType.PlayerKills:
            case QuestObjectiveType.WinPvpPetBattles:
            case QuestObjectiveType.HaveCurrency:
            case QuestObjectiveType.ObtainCurrency:
            case QuestObjectiveType.IncreaseReputation:
                return true;
            
        }

        return false;
    }
}