// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Networking;
using Game.Networking.Packets;

namespace Game;

public partial class WorldSession
{
	[WorldPacketHandler(ClientOpcodes.GetGarrisonInfo)]
	void HandleGetGarrisonInfo(GetGarrisonInfo getGarrisonInfo)
	{
		var garrison = _player.Garrison;

		if (garrison != null)
			garrison.SendInfo();
	}
}