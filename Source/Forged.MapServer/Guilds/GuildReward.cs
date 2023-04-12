using System.Collections.Generic;

namespace Forged.MapServer.Guilds;

public class GuildReward
{
    public List<uint> AchievementsRequired { get; set; } = new();
    public ulong Cost { get; set; }
    public uint ItemID { get; set; }
    public byte MinGuildRep { get; set; }
    public ulong RaceMask { get; set; }
}