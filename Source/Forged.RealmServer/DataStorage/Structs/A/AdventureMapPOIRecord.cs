// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Forged.RealmServer.DataStorage;

public sealed class AdventureMapPOIRecord
{
	public uint Id;
	public LocalizedString Title;
	public string Description;
	public Vector2 WorldPosition;
	public sbyte Type;
	public uint PlayerConditionID;
	public uint QuestID;
	public uint LfgDungeonID;
	public int RewardItemID;
	public uint UiTextureAtlasMemberID;
	public uint UiTextureKitID;
	public int MapID;
	public uint AreaTableID;
}