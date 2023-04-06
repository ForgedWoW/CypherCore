// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Linq;
using Forged.MapServer.Battlepay;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Bpay;
using Forged.MapServer.Server;
using Framework.Constants;

namespace Forged.MapServer.OpCodeHandlers;

public class BattlepayHandler
{
    private readonly BattlePayDataStoreMgr _battlePayDataStoreMgr;
    private readonly BattlepayManager _battlepayManager;
    private readonly WorldSession _session;

    public BattlepayHandler(WorldSession session, BattlepayManager battlepayManager, BattlePayDataStoreMgr battlePayDataStoreMgr)
    {
        _session = session;
        _battlepayManager = battlepayManager;
        _battlePayDataStoreMgr = battlePayDataStoreMgr;
        _battlepayManager.BattlepayHandler = this;
    }

    public void HandleBattlePayAckFailedResponse(BattlePayAckFailedResponse battlePayAckFailedResponse) { }

    public void HandleBattlePayConfirmPurchase(ConfirmPurchaseResponse packet)
    {
        if (!_battlepayManager.IsAvailable)
            return;

        if (_battlepayManager.CurrentTransaction == null)
            return;

        var productInfo = _battlePayDataStoreMgr.GetProductInfoForProduct(_battlepayManager.CurrentTransaction.ProductID);
        var displayInfo = _battlePayDataStoreMgr.GetDisplayInfo(productInfo.Entry);

        if (_battlepayManager.CurrentTransaction.Lock)
        {
            SendPurchaseUpdate(_battlepayManager.CurrentTransaction, BpayError.PurchaseDenied);

            return;
        }

        if (_battlepayManager.CurrentTransaction.ServerToken != packet.ServerToken || !packet.ConfirmPurchase || _battlepayManager.CurrentTransaction.CurrentPrice != packet.ClientCurrentPriceFixedPoint)
        {
            SendPurchaseUpdate(_battlepayManager.CurrentTransaction, BpayError.PurchaseDenied);

            return;
        }

        var accountBalance = _battlepayManager.GetBattlePayCredits();

        if (accountBalance < _battlepayManager.CurrentTransaction.CurrentPrice)
        {
            SendPurchaseUpdate(_battlepayManager.CurrentTransaction, BpayError.PurchaseDenied);

            return;
        }

        _battlepayManager.CurrentTransaction.Lock = true;
        _battlepayManager.CurrentTransaction.Status = (ushort)BpayUpdateStatus.Finish;

        SendPurchaseUpdate(_battlepayManager.CurrentTransaction, BpayError.Other);
        _battlepayManager.SavePurchase(_battlepayManager.CurrentTransaction);
        _battlepayManager.ProcessDelivery(_battlepayManager.CurrentTransaction);
        _battlepayManager.UpdateBattlePayCredits(_battlepayManager.CurrentTransaction.CurrentPrice);

        if (displayInfo.Name1.Length != 0)
            _battlepayManager.SendBattlePayMessage(1, displayInfo.Name1);

        _battlepayManager.SendProductList();
    }

    [WorldPacketHandler(ClientOpcodes.BattlePayDistributionAssignToTarget)]
    public void HandleBattlePayDistributionAssign(DistributionAssignToTarget packet)
    {
        if (!_battlepayManager.IsAvailable)
            return;

        _battlepayManager.AssignDistributionToCharacter(packet.TargetCharacter, packet.DistributionID, packet.ProductID, packet.SpecializationID, packet.ChoiceID);
    }

    public void HandleBattlePayRequestPriceInfo(BattlePayRequestPriceInfo battlePayRequestPriceInfo) { }

    public void HandleBattlePayStartPurchase(StartPurchase packet)
    {
        SendMakePurchase(packet.TargetCharacter, packet.ClientToken, packet.ProductID);
    }

    [WorldPacketHandler(ClientOpcodes.BattlePayGetProductList)]
    public void HandleGetProductList(GetProductList getProductList)
    {
        if (!_battlepayManager.IsAvailable)
            return;

        _battlepayManager.SendProductList();
        _battlepayManager.SendAccountCredits();
    }

    [WorldPacketHandler(ClientOpcodes.BattlePayGetPurchaseList)]
    public void HandleGetPurchaseListQuery(GetPurchaseListQuery getPurchaseListQuery)
    {
        if (!_battlepayManager.IsAvailable)
            return;

        var packet = new PurchaseListResponse(); // @TODO
        _session.SendPacket(packet);
    }

    [WorldPacketHandler(ClientOpcodes.UpdateVasPurchaseStates)]
    public void HandleUpdateVasPurchaseStates(UpdateVasPurchaseStates updateVasPurchaseStates)
    {
        if (!_battlepayManager.IsAvailable)
            return;

        var response = new EnumVasPurchaseStatesResponse
        {
            Result = 0
        };

        _session.SendPacket(response);
    }

    public void SendDisplayPromo(uint promotionID)
    {
        _session.SendPacket(new DisplayPromotion(promotionID));

        if (!_battlepayManager.IsAvailable)
            return;

        if (!_battlePayDataStoreMgr.ProductExist(260))
            return;

        var product = _battlePayDataStoreMgr.GetProduct(260);

        var packet = new DistributionListResponse
        {
            Result = (uint)BpayError.Ok
        };

        var data = new BpayDistributionObject
        {
            TargetPlayer = _session.Player.GUID,
            DistributionID = _battlepayManager.GenerateNewDistributionId(),
            PurchaseID = _battlepayManager.GenerateNewPurchaseID(),
            Status = (uint)BpayDistributionStatus.Available,
            ProductID = 260,
            TargetVirtualRealm = 0,
            TargetNativeRealm = 0,
            Revoked = false
        };

        var productInfo = _battlePayDataStoreMgr.GetProductInfoForProduct(product.ProductId);

        // BATTLEPAY PRODUCTS
        var pProduct = new BpayProduct
        {
            ProductId = product.ProductId,
            Type = product.Type,
            Flags = product.Flags,
            Unk1 = product.Unk1,
            DisplayId = product.DisplayId,
            ItemId = product.ItemId,
            Unk4 = product.Unk4,
            Unk5 = product.Unk5,
            Unk6 = product.Unk6,
            Unk7 = product.Unk7,
            Unk8 = product.Unk8,
            Unk9 = product.Unk9,
            UnkString = product.UnkString,
            UnkBit = product.UnkBit,
            UnkBits = product.UnkBits
        };

        // BATTLEPAY ITEM
        if (product.Items.Count > 0)
            foreach (var pItem in _battlePayDataStoreMgr.GetItemsOfProduct(product.ProductId)
                                                        .Select(item => new BpayProductItem
                                                        {
                                                            ID = item.ID,
                                                            UnkByte = item.UnkByte,
                                                            ItemID = item.ItemID,
                                                            Quantity = item.Quantity,
                                                            UnkInt1 = item.UnkInt1,
                                                            UnkInt2 = item.UnkInt2,
                                                            IsPet = item.IsPet,
                                                            PetResult = item.PetResult
                                                        }))
            {
                if (_battlePayDataStoreMgr.DisplayInfoExist(productInfo.Entry))
                {
                    // productinfo entry and display entry must be the same
                    var dispInfo = _battlepayManager.WriteDisplayInfo(productInfo.Entry);

                    if (dispInfo.Item1)
                        pItem.Display = dispInfo.Item2;
                }

                pProduct.Items.Add(pItem);
            }

        // productinfo entry and display entry must be the same
        var display = _battlepayManager.WriteDisplayInfo(productInfo.Entry);

        if (display.Item1)
            pProduct.Display = display.Item2;

        data.Product = pProduct;

        packet.DistributionObject.Add(data);

        _session.SendPacket(packet);
    }

    public void SendMakePurchase(ObjectGuid targetCharacter, uint clientToken, uint productID)
    {
        var purchase = new Purchase
        {
            ProductID = productID,
            ClientToken = clientToken,
            TargetCharacter = targetCharacter,
            Status = (ushort)BpayUpdateStatus.Loading,
            DistributionId = _battlepayManager.GenerateNewDistributionId()
        };

        var productInfo = _battlePayDataStoreMgr.GetProductInfoForProduct(productID);

        purchase.CurrentPrice = productInfo.CurrentPriceFixedPoint;

        _battlepayManager.RegisterStartPurchase(purchase);

        var accountCredits = _battlepayManager.GetBattlePayCredits();

        if (accountCredits < _battlepayManager.CurrentTransaction.CurrentPrice)
        {
            SendStartPurchaseResponse(_battlepayManager.CurrentTransaction, BpayError.InsufficientBalance);

            return;
        }

        foreach (var productId in productInfo.ProductIds)
            if (_battlePayDataStoreMgr.ProductExist(productId))
            {
                var product = _battlePayDataStoreMgr.GetProduct(productId);

                // if buy is disabled in product addons
                var productAddon = _battlePayDataStoreMgr.GetProductAddon(productInfo.Entry);

                if (productAddon is { DisableBuy: > 0 })
                    SendStartPurchaseResponse(_battlepayManager.CurrentTransaction, BpayError.PurchaseDenied);

                if (product.Items.Count <= 0)
                    continue;

                if (_session.Player != null)
                    if (product.Items.Count > _session.Player.GetFreeBagSlotCount())
                    {
                        _battlepayManager.SendBattlePayMessage(11, product.Name);
                        SendStartPurchaseResponse(_battlepayManager.CurrentTransaction, BpayError.PurchaseDenied);

                        return;
                    }

                if (!product.Items.Any(itr => _battlepayManager.AlreadyOwnProduct(itr.ItemID)))
                    continue;

                _battlepayManager.SendBattlePayMessage(12, product.Name);
                SendStartPurchaseResponse(_battlepayManager.CurrentTransaction, BpayError.PurchaseDenied);

                return;
            }
            else
            {
                SendStartPurchaseResponse(_battlepayManager.CurrentTransaction, BpayError.PurchaseDenied);

                return;
            }

        _battlepayManager.CurrentTransaction.PurchaseID = _battlepayManager.GenerateNewPurchaseID();
        _battlepayManager.CurrentTransaction.ServerToken = RandomHelper.Rand32(0xFFFFFFF);

        SendStartPurchaseResponse(_battlepayManager.CurrentTransaction, BpayError.Ok);
        SendPurchaseUpdate(_battlepayManager.CurrentTransaction, BpayError.Ok);

        var confirmPurchase = new ConfirmPurchase
        {
            PurchaseID = _battlepayManager.CurrentTransaction.PurchaseID,
            ServerToken = _battlepayManager.CurrentTransaction.ServerToken
        };

        _session.SendPacket(confirmPurchase);
    }

    public void SendPurchaseUpdate(Purchase purchase, BpayError result)
    {
        var packet = new PurchaseUpdate();

        var data = new BpayPurchase
        {
            PurchaseID = purchase.PurchaseID,
            UnkLong = 0,
            UnkLong2 = 0,
            Status = purchase.Status,
            ResultCode = (uint)result,
            ProductID = purchase.ProductID,
            UnkInt = purchase.ServerToken,
            WalletName = _battlepayManager.WalletName
        };

        packet.Purchase.Add(data);
        _session.SendPacket(packet);
    }

    public void SendStartPurchaseResponse(Purchase purchase, BpayError result)
    {
        var response = new StartPurchaseResponse
        {
            PurchaseID = purchase.PurchaseID,
            ClientToken = purchase.ClientToken,
            PurchaseResult = (uint)result
        };

        _session.SendPacket(response);
    }

    public void SendSyncWowEntitlements()
    {
        var packet = new SyncWowEntitlements();
        _session.SendPacket(packet);
    }
}