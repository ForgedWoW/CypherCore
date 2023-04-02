// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Collections;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Query;

public class CreatureStats
{
    public int Class;
    public int Classification;
    public int CreatureDifficultyID;
    public int CreatureFamily;
    public uint CreatureMovementInfoID;
    public int CreatureType;
    public string CursorName;
    public CreatureDisplayStats Display = new();
    public float EnergyMulti;
    public uint[] Flags = new uint[2];
    public int HealthScalingExpansion;
    public float HpMulti;
    public bool Leader;
    public StringArray Name = new(SharedConst.MaxCreatureNames);
    public StringArray NameAlt = new(SharedConst.MaxCreatureNames);
    public uint[] ProxyCreatureID = new uint[SharedConst.MaxCreatureKillCredit];
    public List<uint> QuestItems = new();
    public uint RequiredExpansion;
    public string Title;
    public string TitleAlt;
    public uint VignetteID;
    public int WidgetSetID;
    public int WidgetSetUnitConditionID;
}