// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Battlepay;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Bpay;
using Game.Common.Server;

namespace Game.Common.Handlers;

public class BattlepayHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly BattlepayManager _bpayManager;

    public BattlepayHandler(WorldSession session, BattlepayManager bpayManager)
    {
        _session = session;
        _bpayManager = bpayManager;
    }

	public void SendStartPurchaseResponse(WorldSession session, Purchase purchase, BpayError result)
	{
		var response = new StartPurchaseResponse();
		response.PurchaseID = purchase.PurchaseID;
		response.ClientToken = purchase.ClientToken;
		response.PurchaseResult = (uint)result;
		session.SendPacket(response);
	}

    [WorldPacketHandler(ClientOpcodes.BattlePayGetPurchaseList)]
	public void HandleGetPurchaseListQuery(GetPurchaseListQuery UnnamedParameter)
	{
        if (!_bpayManager.IsAvailable())
            return;
        var packet = new PurchaseListResponse(); // @TODO
        _session.SendPacket(packet);
	}

	[WorldPacketHandler(ClientOpcodes.UpdateVasPurchaseStates)]
	public void HandleUpdateVasPurchaseStates(UpdateVasPurchaseStates UnnamedParameter)
	{
        if (!_bpayManager.IsAvailable())
            return;
        var response = new EnumVasPurchaseStatesResponse();
		response.Result = 0;
        _session.SendPacket(response);
	}



	[WorldPacketHandler(ClientOpcodes.BattlePayGetProductList)]
	public void HandleGetProductList(GetProductList UnnamedParameter)
	{
		if (!_bpayManager.IsAvailable())
			return;

		_bpayManager.SendProductList();
		_bpayManager.SendAccountCredits();
	}
	
}
