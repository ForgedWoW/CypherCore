// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Garrison;
using Game.Common.Server;

namespace Game.Common.Handlers;

public class GarrisonHandler
{
    private readonly WorldSession _session;

    public GarrisonHandler(WorldSession session)
    {
        _session = session;
    }

    [WorldPacketHandler(ClientOpcodes.GetGarrisonInfo)]
	void HandleGetGarrisonInfo(GetGarrisonInfo getGarrisonInfo)
	{
		var garrison = _session.Player.Garrison;

		if (garrison != null)
			garrison.SendInfo();
	}
}
