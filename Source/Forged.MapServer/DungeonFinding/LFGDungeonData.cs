// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.DataStorage.Structs.L;
using Framework.Constants;

namespace Forged.MapServer.DungeonFinding;

public class LFGDungeonData
{
    public uint ContentTuningId;
    public Difficulty Difficulty;
    public uint Expansion;
    public uint Group;
    public uint ID;
    public uint Map;
    public string Name;
    public ushort RequiredItemLevel;
    public bool Seasonal;
    public LfgType Type;
    public float X, Y, Z, O;

    public LFGDungeonData(LFGDungeonsRecord dbc)
    {
        ID = dbc.Id;
        Name = dbc.Name[Global.WorldMgr.DefaultDbcLocale];
        Map = (uint)dbc.MapID;
        Type = dbc.TypeID;
        Expansion = dbc.ExpansionLevel;
        Group = dbc.GroupID;
        ContentTuningId = dbc.ContentTuningID;
        Difficulty = dbc.DifficultyID;
        Seasonal = dbc.Flags[0].HasAnyFlag(LfgFlags.Seasonal);
    }

    // Helpers
    public uint Entry()
    {
        return (uint)(ID + ((int)Type << 24));
    }
}