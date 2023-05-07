// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.A;

public sealed record AdventureMapPOIRecord
{
    public uint AreaTableID;
    public string Description;
    public uint Id;
    public uint LfgDungeonID;
    public int MapID;
    public uint PlayerConditionID;
    public uint QuestID;
    public int RewardItemID;
    public LocalizedString Title;
    public sbyte Type;
    public uint UiTextureAtlasMemberID;
    public uint UiTextureKitID;
    public Vector2 WorldPosition;
}