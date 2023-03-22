﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Battlepay;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets.Bpay;

namespace Game;

public partial class WorldSession
{
	public void SendStartPurchaseResponse(WorldSession session, Purchase purchase, BpayError result)
	{
		var response = new StartPurchaseResponse();
		response.PurchaseID = purchase.PurchaseID;
		response.ClientToken = purchase.ClientToken;
		response.PurchaseResult = (uint)result;
		session.SendPacket(response);
	}


	public void SendPurchaseUpdate(WorldSession session, Purchase purchase, BpayError result)
	{
		var packet = new PurchaseUpdate();
		var data = new BpayPurchase();
		data.PurchaseID = purchase.PurchaseID;
		data.UnkLong = 0;
		data.UnkLong2 = 0;
		data.Status = purchase.Status;
		data.ResultCode = (uint)result;
		data.ProductID = purchase.ProductID;
		data.UnkInt = purchase.ServerToken;
		data.WalletName = session.BattlePayMgr.GetDefaultWalletName();
		packet.Purchase.Add(data);
		session.SendPacket(packet);
	}

	[WorldPacketHandler(ClientOpcodes.BattlePayGetPurchaseList)]
	public void HandleGetPurchaseListQuery(GetPurchaseListQuery UnnamedParameter)
	{
        if (!BattlePayMgr.IsAvailable())
            return;
        var packet = new PurchaseListResponse(); // @TODO
		SendPacket(packet);
	}

	[WorldPacketHandler(ClientOpcodes.UpdateVasPurchaseStates)]
	public void HandleUpdateVasPurchaseStates(UpdateVasPurchaseStates UnnamedParameter)
	{
        if (!BattlePayMgr.IsAvailable())
            return;
        var response = new EnumVasPurchaseStatesResponse();
		response.Result = 0;
		SendPacket(response);
	}

	[WorldPacketHandler(ClientOpcodes.BattlePayDistributionAssignToTarget)]
	public void HandleBattlePayDistributionAssign(DistributionAssignToTarget packet)
	{
		if (!BattlePayMgr.IsAvailable())
			return;

		BattlePayMgr.AssignDistributionToCharacter(packet.TargetCharacter, packet.DistributionID, packet.ProductID, packet.SpecializationID, packet.ChoiceID);
	}

	[WorldPacketHandler(ClientOpcodes.BattlePayGetProductList)]
	public void HandleGetProductList(GetProductList UnnamedParameter)
	{
		if (!BattlePayMgr.IsAvailable())
			return;

		BattlePayMgr.SendProductList();
		BattlePayMgr.SendAccountCredits();
	}


	public void SendMakePurchase(ObjectGuid targetCharacter, uint clientToken, uint productID, WorldSession session)
	{
		if (session == null || !session.BattlePayMgr.IsAvailable())
			return;

		var mgr = session.BattlePayMgr;
		var player = session.Player;
		//    auto accountID = session->GetAccountId();

		var purchase = new Purchase();
		purchase.ProductID = productID;
		purchase.ClientToken = clientToken;
		purchase.TargetCharacter = targetCharacter;
		purchase.Status = (ushort)BpayUpdateStatus.Loading;
		purchase.DistributionId = mgr.GenerateNewDistributionId();

		var productInfo = BattlePayDataStoreMgr.Instance.GetProductInfoForProduct(productID);

		purchase.CurrentPrice = (ulong)productInfo.CurrentPriceFixedPoint;

		mgr.RegisterStartPurchase(purchase);

		var accountCredits = BattlePayMgr.GetBattlePayCredits();
		var purchaseData = mgr.GetPurchase();

		if (accountCredits < (ulong)purchaseData.CurrentPrice)
		{
			SendStartPurchaseResponse(session, purchaseData, BpayError.InsufficientBalance);

			return;
		}

		foreach (var productId in productInfo.ProductIds)
			if (BattlePayDataStoreMgr.Instance.ProductExist(productId))
			{
				var product = BattlePayDataStoreMgr.Instance.GetProduct(productId);

				// if buy is disabled in product addons
				var productAddon = BattlePayDataStoreMgr.Instance.GetProductAddon(productInfo.Entry);

				if (productAddon != null)
					if (productAddon.DisableBuy > 0)
						SendStartPurchaseResponse(session, purchaseData, BpayError.PurchaseDenied);

				if (product.Items.Count > 0)
				{
					if (player)
						if (product.Items.Count > player.GetFreeBagSlotCount())
						{
							BattlePayMgr.SendBattlePayMessage(11, product.Name);
							SendStartPurchaseResponse(session, purchaseData, BpayError.PurchaseDenied);

							return;
						}

					foreach (var itr in product.Items)
						if (mgr.AlreadyOwnProduct(itr.ItemID))
						{
							BattlePayMgr.SendBattlePayMessage(12, product.Name);
							SendStartPurchaseResponse(session, purchaseData, BpayError.PurchaseDenied);

							return;
						}
				}
			}
			else
			{
				SendStartPurchaseResponse(session, purchaseData, BpayError.PurchaseDenied);

				return;
			}

		purchaseData.PurchaseID = mgr.GenerateNewPurchaseID();
		purchaseData.ServerToken = RandomHelper.Rand32(0xFFFFFFF);

		SendStartPurchaseResponse(session, purchaseData, BpayError.Ok);
		SendPurchaseUpdate(session, purchaseData, BpayError.Ok);

		var confirmPurchase = new ConfirmPurchase();
		confirmPurchase.PurchaseID = purchaseData.PurchaseID;
		confirmPurchase.ServerToken = purchaseData.ServerToken;
		session.SendPacket(confirmPurchase);
	}

	//C++ TO C# CONVERTER WARNING: The original C++ declaration of the following method implementation was not found:
	public void HandleBattlePayStartPurchase(StartPurchase packet)
	{
		SendMakePurchase(packet.TargetCharacter, packet.ClientToken, packet.ProductID, this);
	}

	//C++ TO C# CONVERTER WARNING: The original C++ declaration of the following method implementation was not found:
	public void HandleBattlePayConfirmPurchase(ConfirmPurchaseResponse packet)
	{
		if (!BattlePayMgr.IsAvailable())
			return;

		var purchase = BattlePayMgr.GetPurchase();

		if (purchase == null)
			return;

		var productInfo = BattlePayDataStoreMgr.Instance.GetProductInfoForProduct(purchase.ProductID);
		var displayInfo = BattlePayDataStoreMgr.Instance.GetDisplayInfo(productInfo.Entry);

		if (purchase.Lock)
		{
			SendPurchaseUpdate(this, purchase, BpayError.PurchaseDenied);

			return;
		}

		if (purchase.ServerToken != packet.ServerToken || !packet.ConfirmPurchase || purchase.CurrentPrice != packet.ClientCurrentPriceFixedPoint)
		{
			SendPurchaseUpdate(this, purchase, BpayError.PurchaseDenied);

			return;
		}

		var accountBalance = BattlePayMgr.GetBattlePayCredits();

		if (accountBalance < purchase.CurrentPrice)
		{
			SendPurchaseUpdate(this, purchase, BpayError.PurchaseDenied);

			return;
		}

		purchase.Lock = true;
		purchase.Status = (ushort)BpayUpdateStatus.Finish;

		SendPurchaseUpdate(this, purchase, BpayError.Other);
		BattlePayMgr.SavePurchase(purchase);
		BattlePayMgr.ProcessDelivery(purchase);
		BattlePayMgr.UpdateBattlePayCredits(purchase.CurrentPrice);

		if (displayInfo.Name1.Length != 0)
			BattlePayMgr.SendBattlePayMessage(1, displayInfo.Name1);

		BattlePayMgr.SendProductList();
	}


	public void HandleBattlePayAckFailedResponse(BattlePayAckFailedResponse UnnamedParameter) { }

	//C++ TO C# CONVERTER WARNING: The original C++ declaration of the following method implementation was not found:
	public void HandleBattlePayRequestPriceInfo(BattlePayRequestPriceInfo UnnamedParameter) { }

	public void SendDisplayPromo(uint promotionID)
	{
		SendPacket(new DisplayPromotion(promotionID));

		if (!BattlePayMgr.IsAvailable())
			return;

		if (!BattlePayDataStoreMgr.Instance.ProductExist(260))
			return;

		var product = BattlePayDataStoreMgr.Instance.GetProduct(260);
		var packet = new DistributionListResponse();
		packet.Result = (uint)BpayError.Ok;

		var data = new BpayDistributionObject();
		data.TargetPlayer = Player.GUID;
		data.DistributionID = BattlePayMgr.GenerateNewDistributionId();
		data.PurchaseID = BattlePayMgr.GenerateNewPurchaseID();
		data.Status = (uint)BpayDistributionStatus.AVAILABLE;
		data.ProductID = 260;
		data.TargetVirtualRealm = 0;
		data.TargetNativeRealm = 0;
		data.Revoked = false;

		var productInfo = BattlePayDataStoreMgr.Instance.GetProductInfoForProduct(product.ProductId);

		// BATTLEPAY PRODUCTS
		var pProduct = new BpayProduct();
		pProduct.ProductId = product.ProductId;
		pProduct.Type = product.Type;
		pProduct.Flags = product.Flags;
		pProduct.Unk1 = product.Unk1;
		pProduct.DisplayId = product.DisplayId;
		pProduct.ItemId = product.ItemId;
		pProduct.Unk4 = product.Unk4;
		pProduct.Unk5 = product.Unk5;
		pProduct.Unk6 = product.Unk6;
		pProduct.Unk7 = product.Unk7;
		pProduct.Unk8 = product.Unk8;
		pProduct.Unk9 = product.Unk9;
		pProduct.UnkString = product.UnkString;
		pProduct.UnkBit = product.UnkBit;
		pProduct.UnkBits = product.UnkBits;

		// BATTLEPAY ITEM
		if (product.Items.Count > 0)
			foreach (var item in BattlePayDataStoreMgr.Instance.GetItemsOfProduct(product.ProductId))
			{
				var pItem = new BpayProductItem();
				pItem.ID = item.ID;
				pItem.UnkByte = item.UnkByte;
				pItem.ItemID = item.ItemID;
				pItem.Quantity = item.Quantity;
				pItem.UnkInt1 = item.UnkInt1;
				pItem.UnkInt2 = item.UnkInt2;
				pItem.IsPet = item.IsPet;
				pItem.PetResult = item.PetResult;

				if (BattlePayDataStoreMgr.Instance.DisplayInfoExist(productInfo.Entry))
				{
					// productinfo entry and display entry must be the same
					var dispInfo = BattlePayMgr.WriteDisplayInfo(productInfo.Entry);

					if (dispInfo.Item1)
						pItem.Display = dispInfo.Item2;
				}

				pProduct.Items.Add(pItem);
			}

		// productinfo entry and display entry must be the same
		var display = BattlePayMgr.WriteDisplayInfo(productInfo.Entry);

		if (display.Item1)
			pProduct.Display = display.Item2;

		data.Product = pProduct;

		packet.DistributionObject.Add(data);

		SendPacket(packet);
	}


	public void SendSyncWowEntitlements()
	{
		var packet = new SyncWowEntitlements();
		SendPacket(packet);
	}
}