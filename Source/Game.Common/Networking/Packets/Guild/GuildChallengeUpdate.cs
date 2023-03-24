// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.Guild;

public class GuildChallengeUpdate : ServerPacket
{
	public int[] CurrentCount = new int[GuildConst.ChallengesTypes];
	public int[] MaxCount = new int[GuildConst.ChallengesTypes];
	public int[] Gold = new int[GuildConst.ChallengesTypes];
	public int[] MaxLevelGold = new int[GuildConst.ChallengesTypes];
	public GuildChallengeUpdate() : base(ServerOpcodes.GuildChallengeUpdate) { }

	public override void Write()
	{
		for (var i = 0; i < GuildConst.ChallengesTypes; ++i)
			_worldPacket.WriteInt32(CurrentCount[i]);

		for (var i = 0; i < GuildConst.ChallengesTypes; ++i)
			_worldPacket.WriteInt32(MaxCount[i]);

		for (var i = 0; i < GuildConst.ChallengesTypes; ++i)
			_worldPacket.WriteInt32(MaxLevelGold[i]);

		for (var i = 0; i < GuildConst.ChallengesTypes; ++i)
			_worldPacket.WriteInt32(Gold[i]);
	}
}
