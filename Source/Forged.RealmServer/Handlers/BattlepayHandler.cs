// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets.Bpay;
using Forged.RealmServer.Handlers;
using Forged.RealmServer.Battlepay;
using Game.Common.Handlers;

namespace Forged.RealmServer;

public class BattlepayHandler : IWorldSessionHandler
{
	private readonly BattlepayManager _battlePayMgr;

    public BattlepayHandler(BattlepayManager battlePayMgr)
    {
        _battlePayMgr = battlePayMgr;
    }

    [WorldPacketHandler(ClientOpcodes.BattlePayDistributionAssignToTarget)]
	public void HandleBattlePayDistributionAssign(DistributionAssignToTarget packet)
	{
		if (!_battlePayMgr.IsAvailable())
			return;

        _battlePayMgr.AssignDistributionToCharacter(packet.TargetCharacter, packet.DistributionID, packet.ProductID, packet.SpecializationID, packet.ChoiceID);
	}

}