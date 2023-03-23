// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Bpay;

namespace Forged.RealmServer;

public partial class WorldSession
{

	[WorldPacketHandler(ClientOpcodes.BattlePayDistributionAssignToTarget)]
	public void HandleBattlePayDistributionAssign(DistributionAssignToTarget packet)
	{
		if (!BattlePayMgr.IsAvailable())
			return;

		BattlePayMgr.AssignDistributionToCharacter(packet.TargetCharacter, packet.DistributionID, packet.ProductID, packet.SpecializationID, packet.ChoiceID);
	}

}