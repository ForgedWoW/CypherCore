// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Common.Networking.Packets.MythicPlus;

public class MythicPlusSeasonData : ServerPacket
{
	public bool SeasonActive;

	public MythicPlusSeasonData() : base(ServerOpcodes.MythicPlusSeasonData) { }

	public override void Write()
	{
		_worldPacket.Write(SeasonActive);
	}
}
