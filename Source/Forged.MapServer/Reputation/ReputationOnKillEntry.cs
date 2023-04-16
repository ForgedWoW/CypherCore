// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Reputation;

public class ReputationOnKillEntry
{
    public bool IsTeamAward1 { get; set; }
    public bool IsTeamAward2 { get; set; }
    public uint RepFaction1 { get; set; }
    public uint RepFaction2 { get; set; }
    public uint ReputationMaxCap1 { get; set; }
    public uint ReputationMaxCap2 { get; set; }
    public int RepValue1 { get; set; }
    public int RepValue2 { get; set; }
    public bool TeamDependent { get; set; }
}