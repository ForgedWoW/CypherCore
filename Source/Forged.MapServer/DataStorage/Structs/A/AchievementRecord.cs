// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.A;

public sealed class AchievementRecord
{
    public ushort Category;
    public int CovenantID;
    public uint CriteriaTree;
    public string Description;
    public AchievementFaction Faction;
    public AchievementFlags Flags;
    public uint IconFileID;
    public uint Id;
    public short InstanceID;
    public byte MinimumCriteria;
    public byte Points;
    public string Reward;
    public int RewardItemID;
    public ushort SharesCriteria;
    public ushort Supercedes;
    public string Title;
    public ushort UiOrder;
}