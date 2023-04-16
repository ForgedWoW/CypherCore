// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Quest;

public class QuestObjective
{
    public int Amount { get; set; }
    public string Description { get; set; }
    public QuestObjectiveFlags Flags { get; set; }
    public uint Flags2 { get; set; }
    public uint Id { get; set; }
    public int ObjectID { get; set; }
    public float ProgressBarWeight { get; set; }
    public uint QuestID { get; set; }
    public sbyte StorageIndex { get; set; }
    public QuestObjectiveType Type { get; set; }
    public int[] VisualEffects { get; set; } = Array.Empty<int>();

    public static bool CanAlwaysBeProgressedInRaid(QuestObjectiveType type)
    {
        return type switch
        {
            QuestObjectiveType.Item               => true,
            QuestObjectiveType.Currency           => true,
            QuestObjectiveType.LearnSpell         => true,
            QuestObjectiveType.MinReputation      => true,
            QuestObjectiveType.MaxReputation      => true,
            QuestObjectiveType.Money              => true,
            QuestObjectiveType.HaveCurrency       => true,
            QuestObjectiveType.IncreaseReputation => true,
            _                                     => false
        };
    }

    public bool IsStoringFlag()
    {
        return Type switch
        {
            QuestObjectiveType.AreaTrigger            => true,
            QuestObjectiveType.WinPetBattleAgainstNpc => true,
            QuestObjectiveType.DefeatBattlePet        => true,
            QuestObjectiveType.CriteriaTree           => true,
            QuestObjectiveType.AreaTriggerEnter       => true,
            QuestObjectiveType.AreaTriggerExit        => true,
            _                                         => false
        };
    }

    public bool IsStoringValue()
    {
        return Type switch
        {
            QuestObjectiveType.Monster            => true,
            QuestObjectiveType.Item               => true,
            QuestObjectiveType.GameObject         => true,
            QuestObjectiveType.TalkTo             => true,
            QuestObjectiveType.PlayerKills        => true,
            QuestObjectiveType.WinPvpPetBattles   => true,
            QuestObjectiveType.HaveCurrency       => true,
            QuestObjectiveType.ObtainCurrency     => true,
            QuestObjectiveType.IncreaseReputation => true,
            _                                     => false
        };
    }
}