// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.BattleGround;

public class RatedPvpInfo : ServerPacket
{
	readonly BracketInfo[] Bracket = new BracketInfo[6];
	public RatedPvpInfo() : base(ServerOpcodes.RatedPvpInfo) { }

	public override void Write()
	{
		foreach (var bracket in Bracket)
			bracket.Write(_worldPacket);
	}
}
