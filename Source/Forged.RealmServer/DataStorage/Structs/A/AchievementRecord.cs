// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.DataStorage;

public sealed class AchievementRecord
{
	public string Description;
	public string Title;
	public string Reward;
	public uint Id;
	public short InstanceID;
	public AchievementFaction Faction;
	public ushort Supercedes;
	public ushort Category;
	public byte MinimumCriteria;
	public byte Points;
	public AchievementFlags Flags;
	public ushort UiOrder;
	public uint IconFileID;
	public int RewardItemID;
	public uint CriteriaTree;
	public ushort SharesCriteria;
	public int CovenantID;
}