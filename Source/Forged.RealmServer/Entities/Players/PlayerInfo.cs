// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.DataStorage;

namespace Forged.RealmServer.Entities;

public class PlayerInfo
{
	public CreatePositionModel CreatePosition;
	public CreatePositionModel? CreatePositionNpe;

	public ItemContext ItemContext { get; set; }
	public List<PlayerCreateInfoItem> Items { get; set; } = new();
	public HashSet<uint> CustomSpells { get; set; } = new();
	public List<uint>[] CastSpells { get; set; } = new List<uint>[(int)PlayerCreateMode.Max];
	public List<PlayerCreateInfoAction> Actions { get; set; } = new();
	public List<SkillRaceClassInfoRecord> Skills { get; set; } = new();

	public uint? IntroMovieId { get; set; }
	public uint? IntroSceneId { get; set; }
	public uint? IntroSceneIdNpe { get; set; }

	public PlayerLevelInfo[] LevelInfo { get; set; } = new PlayerLevelInfo[_worldConfig.GetIntValue(WorldCfg.MaxPlayerLevel)];

	public PlayerInfo()
	{
		for (var i = 0; i < CastSpells.Length; ++i)
			CastSpells[i] = new List<uint>();

		for (var i = 0; i < LevelInfo.Length; ++i)
			LevelInfo[i] = new PlayerLevelInfo();
	}

	public struct CreatePositionModel
	{
		public WorldLocation Loc;
		public ulong? TransportGuid;
	}
}