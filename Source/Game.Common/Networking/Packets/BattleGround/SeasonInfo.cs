// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Common.Networking.Packets.BattleGround;

public class SeasonInfo : ServerPacket
{
	public int MythicPlusDisplaySeasonID;
	public int MythicPlusMilestoneSeasonID;
	public int PreviousArenaSeason;
	public int CurrentArenaSeason;
	public int PvpSeasonID;
	public int ConquestWeeklyProgressCurrencyID;
	public bool WeeklyRewardChestsEnabled;
	public SeasonInfo() : base(ServerOpcodes.SeasonInfo) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(MythicPlusDisplaySeasonID);
		_worldPacket.WriteInt32(MythicPlusMilestoneSeasonID);
		_worldPacket.WriteInt32(CurrentArenaSeason);
		_worldPacket.WriteInt32(PreviousArenaSeason);
		_worldPacket.WriteInt32(ConquestWeeklyProgressCurrencyID);
		_worldPacket.WriteInt32(PvpSeasonID);
		_worldPacket.WriteBit(WeeklyRewardChestsEnabled);
		_worldPacket.FlushBits();
	}
}

//Structs