using System.Collections.Generic;

namespace Forged.MapServer.Guilds;

public class GuildReward
{
    public List<uint> AchievementsRequired = new();
    public ulong Cost;
    public uint ItemID;
    public byte MinGuildRep;
    public ulong RaceMask;
}