﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Entities.Players;

public class PlayerInfo
{
    public CreatePositionModel CreatePosition;
    public CreatePositionModel? CreatePositionNpe;

    public PlayerInfo()
    {
        for (var i = 0; i < CastSpells.Length; ++i)
            CastSpells[i] = new List<uint>();

        for (var i = 0; i < LevelInfo.Length; ++i)
            LevelInfo[i] = new PlayerLevelInfo();
    }

    public List<PlayerCreateInfoAction> Actions { get; set; } = new();
    public List<uint>[] CastSpells { get; set; } = new List<uint>[(int)PlayerCreateMode.Max];
    public HashSet<uint> CustomSpells { get; set; } = new();
    public uint? IntroMovieId { get; set; }
    public uint? IntroSceneId { get; set; }
    public uint? IntroSceneIdNpe { get; set; }
    public ItemContext ItemContext { get; set; }
    public List<PlayerCreateInfoItem> Items { get; set; } = new();
    public PlayerLevelInfo[] LevelInfo { get; set; } = new PlayerLevelInfo[GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel)];
    public List<SkillRaceClassInfoRecord> Skills { get; set; } = new();
    public struct CreatePositionModel
    {
        public WorldLocation Loc;
        public ulong? TransportGuid;
    }
}