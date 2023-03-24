// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Common.Networking.Packets.Bpay;
using Game.Common.Server;

namespace Game.Common.Battlepay;

public class BattlepayManager
{
	private readonly WorldSession _session;
    private readonly BattlePayDataStoreMgr _battlePayDataStoreMgr;

	public BattlepayManager(WorldSession session, BattlePayDataStoreMgr battlePayDataStoreMgr)
	{
		_session = session;
        _battlePayDataStoreMgr = battlePayDataStoreMgr;
	}

	public uint GetShopCurrency()
	{
		return (uint)ConfigMgr.GetDefaultValue("FeatureSystem.BpayStore.Currency", 1);
	}

	public bool IsAvailable()
	{
		return WorldConfig.GetBoolValue(WorldCfg.FeatureSystemBpayStoreEnabled);
	}
	
	
	public Tuple<bool, BpayDisplayInfo> WriteDisplayInfo(uint displayInfoEntry, uint productId = 0)
	{
		//C++ TO C# CONVERTER TASK: Lambda expressions cannot be assigned to 'var':
		var qualityColor = (uint displayInfoOrProductInfoEntry) =>
		{
			var productAddon = _battlePayDataStoreMgr.GetProductAddon(displayInfoOrProductInfoEntry);

			if (productAddon == null)
				return "|cffffffff";

			switch (_battlePayDataStoreMgr.GetProductAddon(displayInfoOrProductInfoEntry).NameColorIndex)
			{
				case 0:
					return "|cffffffff";
				case 1:
					return "|cff1eff00";
				case 2:
					return "|cff0070dd";
				case 3:
					return "|cffa335ee";
				case 4:
					return "|cffff8000";
				case 5:
					return "|cffe5cc80";
				case 6:
					return "|cffe5cc80";
				default:
					return "|cffffffff";
			}
		};

		var info = new BpayDisplayInfo();

		var displayInfo = _battlePayDataStoreMgr.GetDisplayInfo(displayInfoEntry);

		if (displayInfo == null)
			return Tuple.Create(false, info);

		info.CreatureDisplayID = displayInfo.CreatureDisplayID;
		info.VisualID = displayInfo.VisualID;
		info.Name1 = qualityColor(displayInfoEntry) + displayInfo.Name1;
		info.Name2 = displayInfo.Name2;
		info.Name3 = displayInfo.Name3;
		info.Name4 = displayInfo.Name4;
		info.Name5 = displayInfo.Name5;
		info.Name6 = displayInfo.Name6;
		info.Name7 = displayInfo.Name7;
		info.Flags = displayInfo.Flags;
		info.Unk1 = displayInfo.Unk1;
		info.Unk2 = displayInfo.Unk2;
		info.Unk3 = displayInfo.Unk3;
		info.UnkInt1 = displayInfo.UnkInt1;
		info.UnkInt2 = displayInfo.UnkInt2;
		info.UnkInt3 = displayInfo.UnkInt3;

		for (var v = 0; v < displayInfo.Visuals.Count; v++)
		{
			var visual = displayInfo.Visuals[v];

			var _Visual = new BpayVisual();
			_Visual.Name = visual.Name;
			_Visual.DisplayId = visual.DisplayId;
			_Visual.VisualId = visual.VisualId;
			_Visual.Unk = visual.Unk;

			info.Visuals.Add(_Visual);
		}

		if (displayInfo.Flags != 0)
			info.Flags = displayInfo.Flags;

		return Tuple.Create(true, info);
	}

	//C++ TO C# CONVERTER TASK: There is no C# equivalent to C++ suffix return type syntax:
	//ORIGINAL LINE: auto ProductFilter(WorldPackets::BattlePay::Product product)->bool;
	//C++ TO C# CONVERTER TASK: The return type of the following function could not be determined:
	//C++ TO C# CONVERTER TASK: The implementation of the following method could not be found:
	//	auto ProductFilter(WorldPackets::BattlePay::Product product);
	public void SendProductList()
	{
        var response = new ProductListResponse();
		var player = _session.Player; // it's a false value if player is in character screen

		if (!IsAvailable())
		{
			response.Result = (uint)ProductListResult.LockUnk1;
			_session.SendPacket(response);

			return;
		}

		response.Result = (uint)ProductListResult.Available;
		response.CurrencyID = GetShopCurrency() > 0 ? GetShopCurrency() : 1;

		// BATTLEPAY GROUP
		foreach (var itr in _battlePayDataStoreMgr.ProductGroups)
		{
			var group = new BpayGroup();
			group.GroupId = itr.GroupId;
			group.IconFileDataID = itr.IconFileDataID;
			group.DisplayType = itr.DisplayType;
			group.Ordering = itr.Ordering;
			group.Unk = itr.Unk;
			group.Name = itr.Name;
			group.Description = itr.Description;

			response.ProductGroups.Add(group);
		}

		// BATTLEPAY SHOP
		foreach (var itr in _battlePayDataStoreMgr.ShopEntries)
		{
			var shop = new BpayShop();
			shop.EntryId = itr.EntryId;
			shop.GroupID = itr.GroupID;
			shop.ProductID = itr.ProductID;
			shop.Ordering = itr.Ordering;
			shop.VasServiceType = itr.VasServiceType;
			shop.StoreDeliveryType = itr.StoreDeliveryType;

			// shop entry and display entry must be the same
			var data = WriteDisplayInfo(itr.Entry);

			if (data.Item1)
				shop.Display = data.Item2;

			// when logged out don't show everything
			if (player == null && shop.StoreDeliveryType != 2)
				continue;

			var productAddon = _battlePayDataStoreMgr.GetProductAddon(itr.Entry);

			if (productAddon != null)
				if (productAddon.DisableListing > 0)
					continue;

			response.Shops.Add(shop);
		}

		// BATTLEPAY PRODUCT INFO
		foreach (var itr in _battlePayDataStoreMgr.ProductInfos)
		{
			var productInfo = itr.Value;

			var productAddon = _battlePayDataStoreMgr.GetProductAddon(productInfo.Entry);

			if (productAddon != null)
				if (productAddon.DisableListing > 0)
					continue;

			var productinfo = new BpayProductInfo();
			productinfo.ProductId = productInfo.ProductId;
			productinfo.NormalPriceFixedPoint = productInfo.NormalPriceFixedPoint;
			productinfo.CurrentPriceFixedPoint = productInfo.CurrentPriceFixedPoint;
			productinfo.ProductIds = productInfo.ProductIds;
			productinfo.Unk1 = productInfo.Unk1;
			productinfo.Unk2 = productInfo.Unk2;
			productinfo.UnkInts = productInfo.UnkInts;
			productinfo.Unk3 = productInfo.Unk3;
			productinfo.ChoiceType = productInfo.ChoiceType;

			// productinfo entry and display entry must be the same
			var data = WriteDisplayInfo(productInfo.Entry);

			if (data.Item1)
				productinfo.Display = data.Item2;

			response.ProductInfos.Add(productinfo);
		}

		foreach (var itr in _battlePayDataStoreMgr.Products)
		{
			var product = itr.Value;
			var productInfo = _battlePayDataStoreMgr.GetProductInfoForProduct(product.ProductId);

			var productAddon = _battlePayDataStoreMgr.GetProductAddon(productInfo.Entry);

			if (productAddon != null)
				if (productAddon.DisableListing > 0)
					continue;

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
				foreach (var item in _battlePayDataStoreMgr.GetItemsOfProduct(product.ProductId))
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

					if (_battlePayDataStoreMgr.DisplayInfoExist(productInfo.Entry))
					{
						// productinfo entry and display entry must be the same
						var disInfo = WriteDisplayInfo(productInfo.Entry);

						if (disInfo.Item1)
							pItem.Display = disInfo.Item2;
					}

					pProduct.Items.Add(pItem);
				}

			// productinfo entry and display entry must be the same
			var data = WriteDisplayInfo(productInfo.Entry);

			if (data.Item1)
				pProduct.Display = data.Item2;

			response.Products.Add(pProduct);
		}

		/*
		// debug
		TC_LOG_INFO("server.BattlePay", "SendProductList with {} ProductInfos, {} Products, {} Shops. CurrencyID: {}.", response.ProductInfos.size(), response.Products.size(), response.Shops.size(), response.CurrencyID);
		for (int i = 0; i != response.ProductInfos.size(); i++)
		{
			TC_LOG_INFO("server.BattlePay", "({}) ProductInfo: ProductId [{}], First SubProductId [{}], CurrentPriceFixedPoint [{}]", i, response.ProductInfos[i].ProductId, response.ProductInfos[i].ProductIds[0], response.ProductInfos[i].CurrentPriceFixedPoint);
			TC_LOG_INFO("server.BattlePay", "({}) Products: ProductId [{}], UnkString [{}]", i, response.Products[i].ProductId, response.Products[i].UnkString);
			TC_LOG_INFO("server.BattlePay", "({}) Shops: ProductId [{}]", i, response.Shops[i].ProductID);
		}
		*/

		_session.SendPacket(response);
	}

	public void SendAccountCredits()
	{
		//    auto sessionId = _session->GetAccountId();
		//
		//    LoginDatabasePreparedStatement* stmt = DB.Login.GetPreparedStatement(LOGIN_SEL_BATTLE_PAY_ACCOUNT_CREDITS);
		//    stmt->setUInt32(0, _session->GetAccountId());
		//    PreparedQueryResult result = DB.Login.Query(stmt);
		//
		//    auto sSession = sWorld->FindSession(sessionId);
		//    if (!sSession)
		//        return;
		//
		//    uint64 balance = 0;
		//    if (result)
		//    {
		//        auto fields = result->Fetch();
		//        if (auto balanceStr = fields[0].GetCString())
		//            balance = atoi(balanceStr);
		//    }
		//
		//    auto player = sSession->GetPlayer();
		//    if (!player)
		//        return;
		//
		//    SendBattlePayMessage(2, "");
	}
	
}
