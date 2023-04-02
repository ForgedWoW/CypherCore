// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattleGround;

public class SeasonInfo : ServerPacket
{
    public int ConquestWeeklyProgressCurrencyID;
    public int CurrentArenaSeason;
    public int MythicPlusDisplaySeasonID;
    public int MythicPlusMilestoneSeasonID;
    public int PreviousArenaSeason;
    public int PvpSeasonID;
    public bool WeeklyRewardChestsEnabled;
    public SeasonInfo() : base(ServerOpcodes.SeasonInfo) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(MythicPlusDisplaySeasonID);
        WorldPacket.WriteInt32(MythicPlusMilestoneSeasonID);
        WorldPacket.WriteInt32(CurrentArenaSeason);
        WorldPacket.WriteInt32(PreviousArenaSeason);
        WorldPacket.WriteInt32(ConquestWeeklyProgressCurrencyID);
        WorldPacket.WriteInt32(PvpSeasonID);
        WorldPacket.WriteBit(WeeklyRewardChestsEnabled);
        WorldPacket.FlushBits();
    }
}

//Structs