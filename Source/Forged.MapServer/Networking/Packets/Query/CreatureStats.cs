// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Collections;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Query;

public class CreatureStats
{
    public string Title;
    public string TitleAlt;
    public string CursorName;
    public int CreatureType;
    public int CreatureFamily;
    public int Classification;
    public CreatureDisplayStats Display = new();
    public float HpMulti;
    public float EnergyMulti;
    public bool Leader;
    public List<uint> QuestItems = new();
    public uint CreatureMovementInfoID;
    public int HealthScalingExpansion;
    public uint RequiredExpansion;
    public uint VignetteID;
    public int Class;
    public int CreatureDifficultyID;
    public int WidgetSetID;
    public int WidgetSetUnitConditionID;
    public uint[] Flags = new uint[2];
    public uint[] ProxyCreatureID = new uint[SharedConst.MaxCreatureKillCredit];
    public StringArray Name = new(SharedConst.MaxCreatureNames);
    public StringArray NameAlt = new(SharedConst.MaxCreatureNames);
}