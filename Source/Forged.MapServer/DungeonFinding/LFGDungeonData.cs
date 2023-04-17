// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.DataStorage.Structs.L;
using Framework.Constants;

namespace Forged.MapServer.DungeonFinding;

public class LFGDungeonData
{
    public LFGDungeonData(LFGDungeonsRecord dbc, Locale locale)
    {
        ID = dbc.Id;
        Name = dbc.Name[locale];
        Map = (uint)dbc.MapID;
        Type = dbc.TypeID;
        Expansion = dbc.ExpansionLevel;
        Group = dbc.GroupID;
        ContentTuningId = dbc.ContentTuningID;
        Difficulty = dbc.DifficultyID;
        Seasonal = dbc.Flags[0].HasAnyFlag(LfgFlags.Seasonal);
    }

    public uint ContentTuningId { get; set; }
    public Difficulty Difficulty { get; set; }
    public uint Expansion { get; set; }
    public uint Group { get; set; }
    public uint ID { get; set; }
    public uint Map { get; set; }
    public string Name { get; set; }
    public ushort RequiredItemLevel { get; set; }
    public bool Seasonal { get; set; }
    public LfgType Type { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float O { get; set; }

    // Helpers
    public uint Entry()
    {
        return (uint)(ID + ((int)Type << 24));
    }
}