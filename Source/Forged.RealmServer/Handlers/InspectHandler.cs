// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;

namespace Forged.RealmServer;

public partial class WorldSession
{
	[WorldPacketHandler(ClientOpcodes.QueryInspectAchievements, Processing = PacketProcessing.Inplace)]
	void HandleQueryInspectAchievements(QueryInspectAchievements inspect)
	{
		var player = Global.ObjAccessor.GetPlayer(_player, inspect.Guid);

		if (!player)
		{
			Log.outDebug(LogFilter.Network, "WorldSession.HandleQueryInspectAchievements: [{0}] inspected unknown Player [{1}]", Player.GUID.ToString(), inspect.Guid.ToString());

			return;
		}

		if (!Player.IsWithinDistInMap(player, SharedConst.InspectDistance, false))
			return;

		if (Player.IsValidAttackTarget(player))
			return;

		player.SendRespondInspectAchievements(Player);
	}
}