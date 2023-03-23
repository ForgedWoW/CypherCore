// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Game.Entities;
using Game.Spells;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Item;
using Game.Common.Networking.Packets.Trade;
using Game.Common.Server;

namespace Game;

public partial class WorldSession
{
	public void SendTradeStatus(TradeStatusPkt info)
	{
		info.Clear(); // reuse packet
		var trader = Player.GetTrader();
		info.PartnerIsSameBnetAccount = trader && trader.Session.BattlenetAccountId == BattlenetAccountId;
		SendPacket(info);
	}

	public void SendUpdateTrade(bool trader_data = true)
	{
		var view_trade = trader_data ? Player.GetTradeData().GetTraderData() : Player.GetTradeData();

		TradeUpdated tradeUpdated = new();
		tradeUpdated.WhichPlayer = (byte)(trader_data ? 1 : 0);
		tradeUpdated.ClientStateIndex = view_trade.GetClientStateIndex();
		tradeUpdated.CurrentStateIndex = view_trade.GetServerStateIndex();
		tradeUpdated.Gold = view_trade.GetMoney();
		tradeUpdated.ProposedEnchantment = (int)view_trade.GetSpell();

		for (byte i = 0; i < (byte)TradeSlots.Count; ++i)
		{
			var item = view_trade.GetItem((TradeSlots)i);

			if (item)
			{
				TradeUpdated.TradeItem tradeItem = new();
				tradeItem.Slot = i;
				tradeItem.Item = new ItemInstance(item);
				tradeItem.StackCount = (int)item.Count;
				tradeItem.GiftCreator = item.GiftCreator;

				if (!item.IsWrapped)
				{
					TradeUpdated.UnwrappedTradeItem unwrappedItem = new();
					unwrappedItem.EnchantID = (int)item.GetEnchantmentId(EnchantmentSlot.Perm);
					unwrappedItem.OnUseEnchantmentID = (int)item.GetEnchantmentId(EnchantmentSlot.Use);
					unwrappedItem.Creator = item.Creator;
					unwrappedItem.Charges = item.GetSpellCharges();
					unwrappedItem.Lock = item.Template.LockID != 0 && !item.HasItemFlag(ItemFieldFlags.Unlocked);
					unwrappedItem.MaxDurability = item.ItemData.MaxDurability;
					unwrappedItem.Durability = item.ItemData.Durability;

					tradeItem.Unwrapped = unwrappedItem;

					byte g = 0;

					foreach (var gemData in item.ItemData.Gems)
					{
						if (gemData.ItemId != 0)
						{
							ItemGemData gem = new();
							gem.Slot = g;
							gem.Item = new ItemInstance(gemData);
							tradeItem.Unwrapped.Gems.Add(gem);
						}

						++g;
					}
				}

				tradeUpdated.Items.Add(tradeItem);
			}
		}

		SendPacket(tradeUpdated);
	}

	public void SendCancelTrade()
	{
		if (PlayerRecentlyLoggedOut || PlayerLogout)
			return;

		TradeStatusPkt info = new();
		info.Status = TradeStatus.Cancelled;
		SendTradeStatus(info);
	}

	[WorldPacketHandler(ClientOpcodes.IgnoreTrade)]
	void HandleIgnoreTradeOpcode(IgnoreTrade packet) { }

	[WorldPacketHandler(ClientOpcodes.BusyTrade)]
	void HandleBusyTradeOpcode(BusyTrade packet) { }

	void MoveItems(Item[] myItems, Item[] hisItems)
	{
		var trader = Player.GetTrader();

		if (!trader)
			return;

		for (byte i = 0; i < (int)TradeSlots.TradedCount; ++i)
		{
			List<ItemPosCount> traderDst = new();
			List<ItemPosCount> playerDst = new();
			var traderCanTrade = (myItems[i] == null || trader.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, traderDst, myItems[i], false) == InventoryResult.Ok);
			var playerCanTrade = (hisItems[i] == null || Player.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, playerDst, hisItems[i], false) == InventoryResult.Ok);

			if (traderCanTrade && playerCanTrade)
			{
				// Ok, if trade item exists and can be stored
				// If we trade in both directions we had to check, if the trade will work before we actually do it
				// A roll back is not possible after we stored it
				if (myItems[i])
				{
					// logging
					Log.outDebug(LogFilter.Network, "partner storing: {0}", myItems[i].GUID.ToString());

					if (HasPermission(RBACPermissions.LogGmTrade))
						Log.outCommand(_player.Session.AccountId,
										"GM {0} (Account: {1}) trade: {2} (Entry: {3} Count: {4}) to player: {5} (Account: {6})",
										Player.GetName(),
										Player.Session.AccountId,
										myItems[i].Template.GetName(),
										myItems[i].Entry,
										myItems[i].Count,
										trader.GetName(),
										trader.Session.AccountId);

					// adjust time (depends on /played)
					if (myItems[i].IsBOPTradeable)
						myItems[i].SetCreatePlayedTime(trader.TotalPlayedTime - (Player.TotalPlayedTime - myItems[i].ItemData.CreatePlayedTime));

					// store
					trader.MoveItemToInventory(traderDst, myItems[i], true, true);
				}

				if (hisItems[i])
				{
					// logging
					Log.outDebug(LogFilter.Network, "player storing: {0}", hisItems[i].GUID.ToString());

					if (HasPermission(RBACPermissions.LogGmTrade))
						Log.outCommand(trader.Session.AccountId,
										"GM {0} (Account: {1}) trade: {2} (Entry: {3} Count: {4}) to player: {5} (Account: {6})",
										trader.GetName(),
										trader.Session.AccountId,
										hisItems[i].Template.GetName(),
										hisItems[i].Entry,
										hisItems[i].Count,
										Player.GetName(),
										Player.Session.AccountId);


					// adjust time (depends on /played)
					if (hisItems[i].IsBOPTradeable)
						hisItems[i].SetCreatePlayedTime(Player.TotalPlayedTime - (trader.TotalPlayedTime - hisItems[i].ItemData.CreatePlayedTime));

					// store
					Player.MoveItemToInventory(playerDst, hisItems[i], true, true);
				}
			}
			else
			{
				// in case of fatal error log error message
				// return the already removed items to the original owner
				if (myItems[i])
				{
					if (!traderCanTrade)
						Log.outError(LogFilter.Network, "trader can't store item: {0}", myItems[i].GUID.ToString());

					if (Player.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, playerDst, myItems[i], false) == InventoryResult.Ok)
						Player.MoveItemToInventory(playerDst, myItems[i], true, true);
					else
						Log.outError(LogFilter.Network, "player can't take item back: {0}", myItems[i].GUID.ToString());
				}

				// return the already removed items to the original owner
				if (hisItems[i])
				{
					if (!playerCanTrade)
						Log.outError(LogFilter.Network, "player can't store item: {0}", hisItems[i].GUID.ToString());

					if (trader.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, traderDst, hisItems[i], false) == InventoryResult.Ok)
						trader.MoveItemToInventory(traderDst, hisItems[i], true, true);
					else
						Log.outError(LogFilter.Network, "trader can't take item back: {0}", hisItems[i].GUID.ToString());
				}
			}
		}
	}

	static void SetAcceptTradeMode(TradeData myTrade, TradeData hisTrade, Item[] myItems, Item[] hisItems)
	{
		myTrade.SetInAcceptProcess(true);
		hisTrade.SetInAcceptProcess(true);

		// store items in local list and set 'in-trade' flag
		for (byte i = 0; i < (int)TradeSlots.Count; ++i)
		{
			var item = myTrade.GetItem((TradeSlots)i);

			if (item)
			{
				Log.outDebug(LogFilter.Network, "player trade item {0} bag: {1} slot: {2}", item.GUID.ToString(), item.BagSlot, item.Slot);
				//Can return null
				myItems[i] = item;
				myItems[i].SetInTrade();
			}

			item = hisTrade.GetItem((TradeSlots)i);

			if (item)
			{
				Log.outDebug(LogFilter.Network, "partner trade item {0} bag: {1} slot: {2}", item.GUID.ToString(), item.BagSlot, item.Slot);
				hisItems[i] = item;
				hisItems[i].SetInTrade();
			}
		}
	}

	static void ClearAcceptTradeMode(TradeData myTrade, TradeData hisTrade)
	{
		myTrade.SetInAcceptProcess(false);
		hisTrade.SetInAcceptProcess(false);
	}

	static void ClearAcceptTradeMode(Item[] myItems, Item[] hisItems)
	{
		// clear 'in-trade' flag
		for (byte i = 0; i < (int)TradeSlots.Count; ++i)
		{
			if (myItems[i])
				myItems[i].SetInTrade(false);

			if (hisItems[i])
				hisItems[i].SetInTrade(false);
		}
	}

	[WorldPacketHandler(ClientOpcodes.AcceptTrade)]
	void HandleAcceptTrade(AcceptTrade acceptTrade)
	{
		var my_trade = Player.GetTradeData();

		if (my_trade == null)
			return;

		var trader = my_trade.GetTrader();

		var his_trade = trader.GetTradeData();

		if (his_trade == null)
			return;

		var myItems = new Item[(int)TradeSlots.Count];
		var hisItems = new Item[(int)TradeSlots.Count];

		// set before checks for propertly undo at problems (it already set in to client)
		my_trade.SetAccepted(true);

		TradeStatusPkt info = new();

		if (his_trade.GetServerStateIndex() != acceptTrade.StateIndex)
		{
			info.Status = TradeStatus.StateChanged;
			SendTradeStatus(info);
			my_trade.SetAccepted(false);

			return;
		}

		if (!Player.IsWithinDistInMap(trader, 11.11f, false))
		{
			info.Status = TradeStatus.TooFarAway;
			SendTradeStatus(info);
			my_trade.SetAccepted(false);

			return;
		}

		// not accept case incorrect money amount
		if (!Player.HasEnoughMoney(my_trade.GetMoney()))
		{
			info.Status = TradeStatus.Failed;
			info.BagResult = InventoryResult.NotEnoughMoney;
			SendTradeStatus(info);
			my_trade.SetAccepted(false, true);

			return;
		}

		// not accept case incorrect money amount
		if (!trader.HasEnoughMoney(his_trade.GetMoney()))
		{
			info.Status = TradeStatus.Failed;
			info.BagResult = InventoryResult.NotEnoughMoney;
			trader.Session.SendTradeStatus(info);
			his_trade.SetAccepted(false, true);

			return;
		}

		if (Player.Money >= PlayerConst.MaxMoneyAmount - his_trade.GetMoney())
		{
			info.Status = TradeStatus.Failed;
			info.BagResult = InventoryResult.TooMuchGold;
			SendTradeStatus(info);
			my_trade.SetAccepted(false, true);

			return;
		}

		if (trader.Money >= PlayerConst.MaxMoneyAmount - my_trade.GetMoney())
		{
			info.Status = TradeStatus.Failed;
			info.BagResult = InventoryResult.TooMuchGold;
			trader.Session.SendTradeStatus(info);
			his_trade.SetAccepted(false, true);

			return;
		}

		// not accept if some items now can't be trade (cheating)
		for (byte i = 0; i < (byte)TradeSlots.Count; ++i)
		{
			var item = my_trade.GetItem((TradeSlots)i);

			if (item)
			{
				if (!item.CanBeTraded(false, true))
				{
					info.Status = TradeStatus.Cancelled;
					SendTradeStatus(info);

					return;
				}

				if (item.IsBindedNotWith(trader))
				{
					info.Status = TradeStatus.Failed;
					info.BagResult = InventoryResult.TradeBoundItem;
					SendTradeStatus(info);

					return;
				}
			}

			item = his_trade.GetItem((TradeSlots)i);

			if (item)
				if (!item.CanBeTraded(false, true))
				{
					info.Status = TradeStatus.Cancelled;
					SendTradeStatus(info);

					return;
				}
		}

		if (his_trade.IsAccepted())
		{
			SetAcceptTradeMode(my_trade, his_trade, myItems, hisItems);

			Spell my_spell = null;
			SpellCastTargets my_targets = new();

			Spell his_spell = null;
			SpellCastTargets his_targets = new();

			// not accept if spell can't be casted now (cheating)
			var my_spell_id = my_trade.GetSpell();

			if (my_spell_id != 0)
			{
				var spellEntry = Global.SpellMgr.GetSpellInfo(my_spell_id, _player.Map.DifficultyID);
				var castItem = my_trade.GetSpellCastItem();

				if (spellEntry == null ||
					!his_trade.GetItem(TradeSlots.NonTraded) ||
					(my_trade.HasSpellCastItem() && !castItem))
				{
					ClearAcceptTradeMode(my_trade, his_trade);
					ClearAcceptTradeMode(myItems, hisItems);

					my_trade.SetSpell(0);

					return;
				}

				my_spell = new Spell(Player, spellEntry, TriggerCastFlags.FullMask);
				my_spell.CastItem = castItem;
				my_targets.SetTradeItemTarget(Player);
				my_spell.Targets = my_targets;

				var res = my_spell.CheckCast(true);

				if (res != SpellCastResult.SpellCastOk)
				{
					my_spell.SendCastResult(res);

					ClearAcceptTradeMode(my_trade, his_trade);
					ClearAcceptTradeMode(myItems, hisItems);

					my_spell.Dispose();
					my_trade.SetSpell(0);

					return;
				}
			}

			// not accept if spell can't be casted now (cheating)
			var his_spell_id = his_trade.GetSpell();

			if (his_spell_id != 0)
			{
				var spellEntry = Global.SpellMgr.GetSpellInfo(his_spell_id, trader.Map.DifficultyID);
				var castItem = his_trade.GetSpellCastItem();

				if (spellEntry == null || !my_trade.GetItem(TradeSlots.NonTraded) || (his_trade.HasSpellCastItem() && !castItem))
				{
					his_trade.SetSpell(0);

					ClearAcceptTradeMode(my_trade, his_trade);
					ClearAcceptTradeMode(myItems, hisItems);

					return;
				}

				his_spell = new Spell(trader, spellEntry, TriggerCastFlags.FullMask);
				his_spell.CastItem = castItem;
				his_targets.SetTradeItemTarget(trader);
				his_spell.Targets = his_targets;

				var res = his_spell.CheckCast(true);

				if (res != SpellCastResult.SpellCastOk)
				{
					his_spell.SendCastResult(res);

					ClearAcceptTradeMode(my_trade, his_trade);
					ClearAcceptTradeMode(myItems, hisItems);

					my_spell.Dispose();
					his_spell.Dispose();

					his_trade.SetSpell(0);

					return;
				}
			}

			// inform partner client
			info.Status = TradeStatus.Accepted;
			trader.Session.SendTradeStatus(info);

			// test if item will fit in each inventory
			TradeStatusPkt myCanCompleteInfo = new();
			TradeStatusPkt hisCanCompleteInfo = new();
			hisCanCompleteInfo.BagResult = trader.CanStoreItems(myItems, (int)TradeSlots.TradedCount, ref hisCanCompleteInfo.ItemID);
			myCanCompleteInfo.BagResult = Player.CanStoreItems(hisItems, (int)TradeSlots.TradedCount, ref myCanCompleteInfo.ItemID);

			ClearAcceptTradeMode(myItems, hisItems);

			// in case of missing space report error
			if (myCanCompleteInfo.BagResult != InventoryResult.Ok)
			{
				ClearAcceptTradeMode(my_trade, his_trade);

				myCanCompleteInfo.Status = TradeStatus.Failed;
				trader.Session.SendTradeStatus(myCanCompleteInfo);
				myCanCompleteInfo.FailureForYou = true;
				SendTradeStatus(myCanCompleteInfo);
				my_trade.SetAccepted(false);
				his_trade.SetAccepted(false);

				return;
			}
			else if (hisCanCompleteInfo.BagResult != InventoryResult.Ok)
			{
				ClearAcceptTradeMode(my_trade, his_trade);

				hisCanCompleteInfo.Status = TradeStatus.Failed;
				SendTradeStatus(hisCanCompleteInfo);
				hisCanCompleteInfo.FailureForYou = true;
				trader.Session.SendTradeStatus(hisCanCompleteInfo);
				my_trade.SetAccepted(false);
				his_trade.SetAccepted(false);

				return;
			}

			// execute trade: 1. remove
			for (byte i = 0; i < (int)TradeSlots.TradedCount; ++i)
			{
				if (myItems[i])
				{
					myItems[i].SetGiftCreator(Player.GUID);
					Player.MoveItemFromInventory(myItems[i].BagSlot, myItems[i].Slot, true);
				}

				if (hisItems[i])
				{
					hisItems[i].SetGiftCreator(trader.GUID);
					trader.MoveItemFromInventory(hisItems[i].BagSlot, hisItems[i].Slot, true);
				}
			}

			// execute trade: 2. store
			MoveItems(myItems, hisItems);

			// logging money                
			if (HasPermission(RBACPermissions.LogGmTrade))
			{
				if (my_trade.GetMoney() > 0)
					Log.outCommand(Player.Session.AccountId,
									"GM {0} (Account: {1}) give money (Amount: {2}) to player: {3} (Account: {4})",
									Player.GetName(),
									Player.Session.AccountId,
									my_trade.GetMoney(),
									trader.GetName(),
									trader.Session.AccountId);

				if (his_trade.GetMoney() > 0)
					Log.outCommand(Player.Session.AccountId,
									"GM {0} (Account: {1}) give money (Amount: {2}) to player: {3} (Account: {4})",
									trader.GetName(),
									trader.Session.AccountId,
									his_trade.GetMoney(),
									Player.GetName(),
									Player.Session.AccountId);
			}


			// update money
			Player.ModifyMoney(-(long)my_trade.GetMoney());
			Player.ModifyMoney((long)his_trade.GetMoney());
			trader.ModifyMoney(-(long)his_trade.GetMoney());
			trader.ModifyMoney((long)my_trade.GetMoney());

			if (my_spell)
				my_spell.Prepare(my_targets);

			if (his_spell)
				his_spell.Prepare(his_targets);

			// cleanup
			ClearAcceptTradeMode(my_trade, his_trade);
			Player.SetTradeData(null);
			trader.SetTradeData(null);

			// desynchronized with the other saves here (SaveInventoryAndGoldToDB() not have own transaction guards)
			SQLTransaction trans = new();
			Player.SaveInventoryAndGoldToDB(trans);
			trader.SaveInventoryAndGoldToDB(trans);
			DB.Characters.CommitTransaction(trans);

			info.Status = TradeStatus.Complete;
			trader.Session.SendTradeStatus(info);
			SendTradeStatus(info);
		}
		else
		{
			info.Status = TradeStatus.Accepted;
			trader.Session.SendTradeStatus(info);
		}
	}

	[WorldPacketHandler(ClientOpcodes.UnacceptTrade)]
	void HandleUnacceptTrade(UnacceptTrade packet)
	{
		var my_trade = Player.GetTradeData();

		if (my_trade == null)
			return;

		my_trade.SetAccepted(false, true);
	}

	[WorldPacketHandler(ClientOpcodes.BeginTrade)]
	void HandleBeginTrade(BeginTrade packet)
	{
		var my_trade = Player.GetTradeData();

		if (my_trade == null)
			return;

		TradeStatusPkt info = new();
		my_trade.GetTrader().Session.SendTradeStatus(info);
		SendTradeStatus(info);
	}

	[WorldPacketHandler(ClientOpcodes.CancelTrade, Status = SessionStatus.LoggedinOrRecentlyLogout)]
	void HandleCancelTrade(CancelTrade cancelTrade)
	{
		// sent also after LOGOUT COMPLETE
		if (Player) // needed because STATUS_LOGGEDIN_OR_RECENTLY_LOGGOUT
			Player.TradeCancel(true);
	}

	[WorldPacketHandler(ClientOpcodes.InitiateTrade)]
	void HandleInitiateTrade(InitiateTrade initiateTrade)
	{
		if (Player.GetTradeData() != null)
			return;

		TradeStatusPkt info = new();

		if (!Player.IsAlive)
		{
			info.Status = TradeStatus.Dead;
			SendTradeStatus(info);

			return;
		}

		if (Player.HasUnitState(UnitState.Stunned))
		{
			info.Status = TradeStatus.Stunned;
			SendTradeStatus(info);

			return;
		}

		if (IsLogingOut)
		{
			info.Status = TradeStatus.LoggingOut;
			SendTradeStatus(info);

			return;
		}

		if (Player.IsInFlight)
		{
			info.Status = TradeStatus.TooFarAway;
			SendTradeStatus(info);

			return;
		}

		if (Player.Level < WorldConfig.GetIntValue(WorldCfg.TradeLevelReq))
		{
			SendNotification(Global.ObjectMgr.GetCypherString(CypherStrings.TradeReq), WorldConfig.GetIntValue(WorldCfg.TradeLevelReq));
			info.Status = TradeStatus.Failed;
			SendTradeStatus(info);

			return;
		}


		var pOther = Global.ObjAccessor.FindPlayer(initiateTrade.Guid);

		if (!pOther)
		{
			info.Status = TradeStatus.NoTarget;
			SendTradeStatus(info);

			return;
		}

		if (pOther == Player || pOther.GetTradeData() != null)
		{
			info.Status = TradeStatus.PlayerBusy;
			SendTradeStatus(info);

			return;
		}

		if (!pOther.IsAlive)
		{
			info.Status = TradeStatus.TargetDead;
			SendTradeStatus(info);

			return;
		}

		if (pOther.IsInFlight)
		{
			info.Status = TradeStatus.TooFarAway;
			SendTradeStatus(info);

			return;
		}

		if (pOther.HasUnitState(UnitState.Stunned))
		{
			info.Status = TradeStatus.TargetStunned;
			SendTradeStatus(info);

			return;
		}

		if (pOther.Session.IsLogingOut)
		{
			info.Status = TradeStatus.TargetLoggingOut;
			SendTradeStatus(info);

			return;
		}

		if (pOther.Social.HasIgnore(Player.GUID, Player.Session.AccountGUID))
		{
			info.Status = TradeStatus.PlayerIgnored;
			SendTradeStatus(info);

			return;
		}

		if ((pOther.Team != Player.Team ||
			pOther.HasPlayerFlagEx(PlayerFlagsEx.MercenaryMode) ||
			Player.HasPlayerFlagEx(PlayerFlagsEx.MercenaryMode)) &&
			(!WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideTrade) &&
			!HasPermission(RBACPermissions.AllowTwoSideTrade)))
		{
			info.Status = TradeStatus.WrongFaction;
			SendTradeStatus(info);

			return;
		}

		if (!pOther.IsWithinDistInMap(Player, 11.11f, false))
		{
			info.Status = TradeStatus.TooFarAway;
			SendTradeStatus(info);

			return;
		}

		if (pOther.Level < WorldConfig.GetIntValue(WorldCfg.TradeLevelReq))
		{
			SendNotification(Global.ObjectMgr.GetCypherString(CypherStrings.TradeOtherReq), WorldConfig.GetIntValue(WorldCfg.TradeLevelReq));
			info.Status = TradeStatus.Failed;
			SendTradeStatus(info);

			return;
		}

		// OK start trade
		Player.SetTradeData(new TradeData(Player, pOther));
		pOther.SetTradeData(new TradeData(pOther, Player));

		info.Status = TradeStatus.Proposed;
		info.Partner = Player.GUID;
		pOther.Session.SendTradeStatus(info);
	}

	[WorldPacketHandler(ClientOpcodes.SetTradeGold)]
	void HandleSetTradeGold(SetTradeGold setTradeGold)
	{
		var my_trade = Player.GetTradeData();

		if (my_trade == null)
			return;

		my_trade.UpdateClientStateIndex();
		my_trade.SetMoney(setTradeGold.Coinage);
	}

	[WorldPacketHandler(ClientOpcodes.SetTradeItem)]
	void HandleSetTradeItem(SetTradeItem setTradeItem)
	{
		var my_trade = Player.GetTradeData();

		if (my_trade == null)
			return;

		TradeStatusPkt info = new();

		// invalid slot number
		if (setTradeItem.TradeSlot >= (byte)TradeSlots.Count)
		{
			info.Status = TradeStatus.Cancelled;
			SendTradeStatus(info);

			return;
		}

		// check cheating, can't fail with correct client operations
		var item = Player.GetItemByPos(setTradeItem.PackSlot, setTradeItem.ItemSlotInPack);

		if (!item || (setTradeItem.TradeSlot != (byte)TradeSlots.NonTraded && !item.CanBeTraded(false, true)))
		{
			info.Status = TradeStatus.Cancelled;
			SendTradeStatus(info);

			return;
		}

		var iGUID = item.GUID;

		// prevent place single item into many trade slots using cheating and client bugs
		if (my_trade.HasItem(iGUID))
		{
			// cheating attempt
			info.Status = TradeStatus.Cancelled;
			SendTradeStatus(info);

			return;
		}

		my_trade.UpdateClientStateIndex();

		if (setTradeItem.TradeSlot != (byte)TradeSlots.NonTraded && item.IsBindedNotWith(my_trade.GetTrader()))
		{
			info.Status = TradeStatus.NotOnTaplist;
			info.TradeSlot = setTradeItem.TradeSlot;
			SendTradeStatus(info);

			return;
		}

		my_trade.SetItem((TradeSlots)setTradeItem.TradeSlot, item);
	}

	[WorldPacketHandler(ClientOpcodes.ClearTradeItem)]
	void HandleClearTradeItem(ClearTradeItem clearTradeItem)
	{
		var my_trade = Player.GetTradeData();

		if (my_trade == null)
			return;

		my_trade.UpdateClientStateIndex();

		// invalid slot number
		if (clearTradeItem.TradeSlot >= (byte)TradeSlots.Count)
			return;

		my_trade.SetItem((TradeSlots)clearTradeItem.TradeSlot, null);
	}

	[WorldPacketHandler(ClientOpcodes.SetTradeCurrency)]
	void HandleSetTradeCurrency(SetTradeCurrency setTradeCurrency) { }
}