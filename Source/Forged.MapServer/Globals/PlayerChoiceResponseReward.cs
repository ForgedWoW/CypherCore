using System.Collections.Generic;

namespace Forged.MapServer.Globals;

public class PlayerChoiceResponseReward
{
    public int TitleId;
    public int PackageId;
    public int SkillLineId;
    public uint SkillPointCount;
    public uint ArenaPointCount;
    public uint HonorPointCount;
    public ulong Money;
    public uint Xp;

    public List<PlayerChoiceResponseRewardItem> Items = new();
    public List<PlayerChoiceResponseRewardEntry> Currency = new();
    public List<PlayerChoiceResponseRewardEntry> Faction = new();
    public List<PlayerChoiceResponseRewardItem> ItemChoices = new();
}