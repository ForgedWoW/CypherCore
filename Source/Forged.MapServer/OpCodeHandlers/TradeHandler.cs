// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Chat.Commands;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Item;
using Forged.MapServer.Networking.Packets.Trade;
using Forged.MapServer.Server;
using Forged.MapServer.Spells;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Game.Common.Handlers;
using Microsoft.Extensions.Configuration;
using Serilog;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class TradeHandler : IWorldSessionHandler
{
    private readonly CharacterDatabase _characterDatabase;
    private readonly IConfiguration _configuration;
    private readonly GameObjectManager _gameObjectManager;
    private readonly ObjectAccessor _objectAccessor;
    private readonly WorldSession _session;
    private readonly SpellManager _spellManager;

    public TradeHandler(WorldSession session, SpellManager spellManager, CharacterDatabase characterDatabase, IConfiguration configuration,
                        GameObjectManager gameObjectManager, ObjectAccessor objectAccessor)
    {
        _session = session;
        _spellManager = spellManager;
        _characterDatabase = characterDatabase;
        _configuration = configuration;
        _gameObjectManager = gameObjectManager;
        _objectAccessor = objectAccessor;
    }

    public void SendCancelTrade()
    {
        if (_session.PlayerRecentlyLoggedOut || _session.PlayerLogout)
            return;

        TradeStatusPkt info = new()
        {
            Status = TradeStatus.Cancelled
        };

        SendTradeStatus(info);
    }

    public void SendTradeStatus(TradeStatusPkt info)
    {
        info.Clear(); // reuse packet
        var trader = _session.Player.Trader;
        info.PartnerIsSameBnetAccount = trader != null && trader.Session.BattlenetAccountId == _session.BattlenetAccountId;
        _session.SendPacket(info);
    }

    public void SendUpdateTrade(bool traderData = true)
    {
        var viewTrade = traderData ? _session.Player.TradeData.TraderData : _session.Player.TradeData;

        TradeUpdated tradeUpdated = new()
        {
            WhichPlayer = (byte)(traderData ? 1 : 0),
            ClientStateIndex = viewTrade.ClientStateIndex,
            CurrentStateIndex = viewTrade.ServerStateIndex,
            Gold = viewTrade.Money,
            ProposedEnchantment = (int)viewTrade.Spell
        };

        for (byte i = 0; i < (byte)TradeSlots.Count; ++i)
        {
            var item = viewTrade.GetItem((TradeSlots)i);

            if (item == null)
                continue;

            TradeUpdated.TradeItem tradeItem = new()
            {
                Slot = i,
                Item = new ItemInstance(item),
                StackCount = (int)item.Count,
                GiftCreator = item.GiftCreator
            };

            if (!item.IsWrapped)
            {
                TradeUpdated.UnwrappedTradeItem unwrappedItem = new()
                {
                    EnchantID = (int)item.GetEnchantmentId(EnchantmentSlot.Perm),
                    OnUseEnchantmentID = (int)item.GetEnchantmentId(EnchantmentSlot.Use),
                    Creator = item.Creator,
                    Charges = item.GetSpellCharges(),
                    Lock = item.Template.LockID != 0 && !item.HasItemFlag(ItemFieldFlags.Unlocked),
                    MaxDurability = item.ItemData.MaxDurability,
                    Durability = item.ItemData.Durability
                };

                tradeItem.Unwrapped = unwrappedItem;

                byte g = 0;

                foreach (var gemData in item.ItemData.Gems)
                {
                    if (gemData.ItemId != 0)
                    {
                        ItemGemData gem = new()
                        {
                            Slot = g,
                            Item = new ItemInstance(gemData)
                        };

                        tradeItem.Unwrapped.Gems.Add(gem);
                    }

                    ++g;
                }
            }

            tradeUpdated.Items.Add(tradeItem);
        }

        _session.SendPacket(tradeUpdated);
    }

    private static void ClearAcceptTradeMode(TradeData myTrade, TradeData hisTrade)
    {
        myTrade.SetInAcceptProcess(false);
        hisTrade.SetInAcceptProcess(false);
    }

    private static void ClearAcceptTradeMode(Item[] myItems, Item[] hisItems)
    {
        // clear 'in-trade' Id
        for (byte i = 0; i < (int)TradeSlots.Count; ++i)
        {
            if (myItems[i] != null)
                myItems[i].SetInTrade(false);

            if (hisItems[i] != null)
                hisItems[i].SetInTrade(false);
        }
    }

    private static void SetAcceptTradeMode(TradeData myTrade, TradeData hisTrade, Item[] myItems, Item[] hisItems)
    {
        myTrade.SetInAcceptProcess(true);
        hisTrade.SetInAcceptProcess(true);

        // store items in local list and set 'in-trade' Id
        for (byte i = 0; i < (int)TradeSlots.Count; ++i)
        {
            var item = myTrade.GetItem((TradeSlots)i);

            if (item != null)
            {
                Log.Logger.Debug("player trade item {0} bag: {1} slot: {2}", item.GUID.ToString(), item.BagSlot, item.Slot);
                //Can return null
                myItems[i] = item;
                myItems[i].SetInTrade();
            }

            item = hisTrade.GetItem((TradeSlots)i);

            if (item == null)
                continue;

            Log.Logger.Debug("partner trade item {0} bag: {1} slot: {2}", item.GUID.ToString(), item.BagSlot, item.Slot);
            hisItems[i] = item;
            hisItems[i].SetInTrade();
        }
    }

    [WorldPacketHandler(ClientOpcodes.AcceptTrade)]
    private void HandleAcceptTrade(AcceptTrade acceptTrade)
    {
        var myTrade = _session.Player.TradeData;

        if (myTrade == null || myTrade.Trader.Session.PacketRouter.TryGetOpCodeHandler(out TradeHandler traderTradeHandler))
            return;

        var hisTrade = myTrade.Trader.TradeData;

        if (hisTrade == null)
            return;

        var myItems = new Item[(int)TradeSlots.Count];
        var hisItems = new Item[(int)TradeSlots.Count];

        // set before checks for propertly undo at problems (it already set in to client)
        myTrade.SetAccepted(true);

        TradeStatusPkt info = new();

        if (hisTrade.ServerStateIndex != acceptTrade.StateIndex)
        {
            info.Status = TradeStatus.StateChanged;
            SendTradeStatus(info);
            myTrade.SetAccepted(false);

            return;
        }

        if (!_session.Player.Location.IsWithinDistInMap(myTrade.Trader, 11.11f, false))
        {
            info.Status = TradeStatus.TooFarAway;
            SendTradeStatus(info);
            myTrade.SetAccepted(false);

            return;
        }

        // not accept case incorrect money amount
        if (!_session.Player.HasEnoughMoney(myTrade.Money))
        {
            info.Status = TradeStatus.Failed;
            info.BagResult = InventoryResult.NotEnoughMoney;
            SendTradeStatus(info);
            myTrade.SetAccepted(false, true);

            return;
        }

        // not accept case incorrect money amount
        if (!myTrade.Trader.HasEnoughMoney(hisTrade.Money))
        {
            info.Status = TradeStatus.Failed;
            info.BagResult = InventoryResult.NotEnoughMoney;
            traderTradeHandler.SendTradeStatus(info);
            hisTrade.SetAccepted(false, true);

            return;
        }

        if (_session.Player.Money >= PlayerConst.MaxMoneyAmount - hisTrade.Money)
        {
            info.Status = TradeStatus.Failed;
            info.BagResult = InventoryResult.TooMuchGold;
            SendTradeStatus(info);
            myTrade.SetAccepted(false, true);

            return;
        }

        if (myTrade.Trader.Money >= PlayerConst.MaxMoneyAmount - myTrade.Money)
        {
            info.Status = TradeStatus.Failed;
            info.BagResult = InventoryResult.TooMuchGold;
            traderTradeHandler.SendTradeStatus(info);
            hisTrade.SetAccepted(false, true);

            return;
        }

        // not accept if some items now can't be trade (cheating)
        for (byte i = 0; i < (byte)TradeSlots.Count; ++i)
        {
            var item = myTrade.GetItem((TradeSlots)i);

            if (item != null)
            {
                if (!item.CanBeTraded(false, true))
                {
                    info.Status = TradeStatus.Cancelled;
                    SendTradeStatus(info);

                    return;
                }

                if (item.IsBindedNotWith(myTrade.Trader))
                {
                    info.Status = TradeStatus.Failed;
                    info.BagResult = InventoryResult.TradeBoundItem;
                    SendTradeStatus(info);

                    return;
                }
            }

            item = hisTrade.GetItem((TradeSlots)i);

            if (item == null || item.CanBeTraded(false, true))
                continue;

            info.Status = TradeStatus.Cancelled;
            SendTradeStatus(info);

            return;
        }

        if (hisTrade.IsAccepted)
        {
            SetAcceptTradeMode(myTrade, hisTrade, myItems, hisItems);

            Spell mySpell = null;
            SpellCastTargets myTargets = new();

            Spell hisSpell = null;
            SpellCastTargets hisTargets = new();

            // not accept if spell can't be casted now (cheating)
            var mySpellID = myTrade.Spell;

            if (mySpellID != 0)
            {
                var spellEntry = _spellManager.GetSpellInfo(mySpellID, _session.Player.Location.Map.DifficultyID);
                var castItem = myTrade.SpellCastItem;

                if (spellEntry == null ||
                    hisTrade.GetItem(TradeSlots.NonTraded) == null ||
                    (myTrade.HasSpellCastItem && castItem == null))
                {
                    ClearAcceptTradeMode(myTrade, hisTrade);
                    ClearAcceptTradeMode(myItems, hisItems);

                    myTrade.SetSpell(0);

                    return;
                }

                mySpell = _session.Player.SpellFactory.NewSpell(spellEntry, TriggerCastFlags.FullMask);
                mySpell.CastItem = castItem;
                myTargets.SetTradeItemTarget(_session.Player);
                mySpell.Targets = myTargets;

                var res = mySpell.CheckCast(true);

                if (res != SpellCastResult.SpellCastOk)
                {
                    mySpell.SendCastResult(res);

                    ClearAcceptTradeMode(myTrade, hisTrade);
                    ClearAcceptTradeMode(myItems, hisItems);

                    mySpell.Dispose();
                    myTrade.SetSpell(0);

                    return;
                }
            }

            // not accept if spell can't be casted now (cheating)
            var hisSpellID = hisTrade.Spell;

            if (hisSpellID != 0)
            {
                var spellEntry = _spellManager.GetSpellInfo(hisSpellID, myTrade.Trader.Location.Map.DifficultyID);
                var castItem = hisTrade.SpellCastItem;

                if (spellEntry == null || myTrade.GetItem(TradeSlots.NonTraded) == null || (hisTrade.HasSpellCastItem && castItem == null))
                {
                    hisTrade.SetSpell(0);

                    ClearAcceptTradeMode(myTrade, hisTrade);
                    ClearAcceptTradeMode(myItems, hisItems);

                    return;
                }

                hisSpell = myTrade.Trader.SpellFactory.NewSpell(spellEntry, TriggerCastFlags.FullMask);
                hisSpell.CastItem = castItem;
                hisTargets.SetTradeItemTarget(myTrade.Trader);
                hisSpell.Targets = hisTargets;

                var res = hisSpell.CheckCast(true);

                if (res != SpellCastResult.SpellCastOk)
                {
                    hisSpell.SendCastResult(res);

                    ClearAcceptTradeMode(myTrade, hisTrade);
                    ClearAcceptTradeMode(myItems, hisItems);

                    mySpell?.Dispose();
                    hisSpell.Dispose();

                    hisTrade.SetSpell(0);

                    return;
                }
            }

            // inform partner client
            info.Status = TradeStatus.Accepted;
            traderTradeHandler.SendTradeStatus(info);

            // test if item will fit in each inventory
            TradeStatusPkt myCanCompleteInfo = new();
            TradeStatusPkt hisCanCompleteInfo = new();
            hisCanCompleteInfo.BagResult = myTrade.Trader.CanStoreItems(myItems, (int)TradeSlots.TradedCount, ref hisCanCompleteInfo.ItemID);
            myCanCompleteInfo.BagResult = _session.Player.CanStoreItems(hisItems, (int)TradeSlots.TradedCount, ref myCanCompleteInfo.ItemID);

            ClearAcceptTradeMode(myItems, hisItems);

            // in case of missing space report error
            if (myCanCompleteInfo.BagResult != InventoryResult.Ok)
            {
                ClearAcceptTradeMode(myTrade, hisTrade);

                myCanCompleteInfo.Status = TradeStatus.Failed;
                traderTradeHandler.SendTradeStatus(myCanCompleteInfo);
                myCanCompleteInfo.FailureForYou = true;
                SendTradeStatus(myCanCompleteInfo);
                myTrade.SetAccepted(false);
                hisTrade.SetAccepted(false);

                return;
            }

            if (hisCanCompleteInfo.BagResult != InventoryResult.Ok)
            {
                ClearAcceptTradeMode(myTrade, hisTrade);

                hisCanCompleteInfo.Status = TradeStatus.Failed;
                SendTradeStatus(hisCanCompleteInfo);
                hisCanCompleteInfo.FailureForYou = true;
                traderTradeHandler.SendTradeStatus(hisCanCompleteInfo);
                myTrade.SetAccepted(false);
                hisTrade.SetAccepted(false);

                return;
            }

            // execute trade: 1. remove
            for (byte i = 0; i < (int)TradeSlots.TradedCount; ++i)
            {
                if (myItems[i] != null)
                {
                    myItems[i].SetGiftCreator(_session.Player.GUID);
                    _session.Player.MoveItemFromInventory(myItems[i].BagSlot, myItems[i].Slot, true);
                }

                if (hisItems[i] == null)
                    continue;

                hisItems[i].SetGiftCreator(myTrade.Trader.GUID);
                myTrade.Trader.MoveItemFromInventory(hisItems[i].BagSlot, hisItems[i].Slot, true);
            }

            // execute trade: 2. store
            MoveItems(myItems, hisItems);

            // logging money
            if (_session.HasPermission(RBACPermissions.LogGmTrade))
            {
                if (myTrade.Money > 0)
                    Log.Logger.ForContext<GMCommands>().Information("GM {0} (Account: {1}) give money (Amount: {2}) to player: {3} (Account: {4})",
                                                                    _session.Player.GetName(),
                                                                    _session.Player.Session.AccountId,
                                                                    myTrade.Money,
                                                                    myTrade.Trader.GetName(),
                                                                    myTrade.Trader.Session.AccountId);

                if (hisTrade.Money > 0)
                    Log.Logger.ForContext<GMCommands>().Information("GM {0} (Account: {1}) give money (Amount: {2}) to player: {3} (Account: {4})",
                                                                    myTrade.Trader.GetName(),
                                                                    myTrade.Trader.Session.AccountId,
                                                                    hisTrade.Money,
                                                                    _session.Player.GetName(),
                                                                    _session.Player.Session.AccountId);
            }

            // update money
            _session.Player.ModifyMoney(-(long)myTrade.Money);
            _session.Player.ModifyMoney((long)hisTrade.Money);
            myTrade.Trader.ModifyMoney(-(long)hisTrade.Money);
            myTrade.Trader.ModifyMoney((long)myTrade.Money);

            mySpell?.Prepare(myTargets);
            hisSpell?.Prepare(hisTargets);

            // cleanup
            ClearAcceptTradeMode(myTrade, hisTrade);
            _session.Player.SetTradeData(null);
            myTrade.Trader.SetTradeData(null);

            // desynchronized with the other saves here (SaveInventoryAndGoldToDB() not have own transaction guards)
            SQLTransaction trans = new();
            _session.Player.SaveInventoryAndGoldToDB(trans);
            myTrade.Trader.SaveInventoryAndGoldToDB(trans);
            _characterDatabase.CommitTransaction(trans);

            info.Status = TradeStatus.Complete;
            traderTradeHandler.SendTradeStatus(info);
            SendTradeStatus(info);
        }
        else
        {
            info.Status = TradeStatus.Accepted;
            traderTradeHandler.SendTradeStatus(info);
        }
    }

    [WorldPacketHandler(ClientOpcodes.BeginTrade)]
    private void HandleBeginTrade(BeginTrade packet)
    {
        if (_session.Player.TradeData == null || packet == null || _session.PacketRouter.TryGetOpCodeHandler(out TradeHandler handler))
            return;

        TradeStatusPkt info = new();
        handler.SendTradeStatus(info);
        SendTradeStatus(info);
    }

    [WorldPacketHandler(ClientOpcodes.BusyTrade)]
    private void HandleBusyTradeOpcode(BusyTrade packet)
    {
        if (packet != null)
        {
        }
    }

    [WorldPacketHandler(ClientOpcodes.CancelTrade, Status = SessionStatus.LoggedinOrRecentlyLogout)]
    private void HandleCancelTrade(CancelTrade cancelTrade)
    {
        if (cancelTrade == null)
            return;

        // sent also after LOGOUT COMPLETE
        _session.Player?.TradeCancel(true);
    }

    [WorldPacketHandler(ClientOpcodes.ClearTradeItem)]
    private void HandleClearTradeItem(ClearTradeItem clearTradeItem)
    {
        if (_session.Player.TradeData == null)
            return;

        _session.Player.TradeData.UpdateClientStateIndex();

        // invalid slot number
        if (clearTradeItem.TradeSlot >= (byte)TradeSlots.Count)
            return;

        _session.Player.TradeData.SetItem((TradeSlots)clearTradeItem.TradeSlot, null);
    }

    [WorldPacketHandler(ClientOpcodes.IgnoreTrade)]
    private void HandleIgnoreTradeOpcode(IgnoreTrade packet)
    {
        if (packet != null)
        {
        }
    }

    [WorldPacketHandler(ClientOpcodes.InitiateTrade)]
    private void HandleInitiateTrade(InitiateTrade initiateTrade)
    {
        if (_session.Player.TradeData != null)
            return;

        TradeStatusPkt info = new();

        if (!_session.Player.IsAlive)
        {
            info.Status = TradeStatus.Dead;
            SendTradeStatus(info);

            return;
        }

        if (_session.Player.HasUnitState(UnitState.Stunned))
        {
            info.Status = TradeStatus.Stunned;
            SendTradeStatus(info);

            return;
        }

        if (_session.IsLogingOut)
        {
            info.Status = TradeStatus.LoggingOut;
            SendTradeStatus(info);

            return;
        }

        if (_session.Player.IsInFlight)
        {
            info.Status = TradeStatus.TooFarAway;
            SendTradeStatus(info);

            return;
        }

        if (_session.Player.Level < _configuration.GetDefaultValue("LevelReq:Trade", 1))
        {
            _session.SendNotification(_gameObjectManager.GetCypherString(CypherStrings.TradeReq), _configuration.GetDefaultValue("LevelReq:Trade", 1));
            info.Status = TradeStatus.Failed;
            SendTradeStatus(info);

            return;
        }

        var pOther = _objectAccessor.FindPlayer(initiateTrade.Guid);

        if (pOther == null)
        {
            info.Status = TradeStatus.NoTarget;
            SendTradeStatus(info);

            return;
        }

        if (pOther == _session.Player || pOther.TradeData != null)
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

        if (pOther.Social.HasIgnore(_session.Player.GUID, _session.Player.Session.AccountGUID))
        {
            info.Status = TradeStatus.PlayerIgnored;
            SendTradeStatus(info);

            return;
        }

        if ((pOther.Team != _session.Player.Team ||
             pOther.HasPlayerFlagEx(PlayerFlagsEx.MercenaryMode) ||
             _session.Player.HasPlayerFlagEx(PlayerFlagsEx.MercenaryMode)) &&
            (!_configuration.GetDefaultValue("AllowTwoSide:Trade", false) &&
             !_session.HasPermission(RBACPermissions.AllowTwoSideTrade)))
        {
            info.Status = TradeStatus.WrongFaction;
            SendTradeStatus(info);

            return;
        }

        if (!pOther.Location.IsWithinDistInMap(_session.Player, 11.11f, false))
        {
            info.Status = TradeStatus.TooFarAway;
            SendTradeStatus(info);

            return;
        }

        if (pOther.Level < _configuration.GetDefaultValue("LevelReq:Trade", 1))
        {
            _session.SendNotification(_gameObjectManager.GetCypherString(CypherStrings.TradeOtherReq), _configuration.GetDefaultValue("LevelReq:Trade", 1));
            info.Status = TradeStatus.Failed;
            SendTradeStatus(info);

            return;
        }

        // OK start trade
        _session.Player.SetTradeData(new TradeData(_session.Player, pOther));
        pOther.SetTradeData(new TradeData(pOther, _session.Player));

        info.Status = TradeStatus.Proposed;
        info.Partner = _session.Player.GUID;

        if (pOther.Session.PacketRouter.TryGetOpCodeHandler(out TradeHandler handler))
            handler.SendTradeStatus(info);
    }

    [WorldPacketHandler(ClientOpcodes.SetTradeCurrency)]
    private void HandleSetTradeCurrency(SetTradeCurrency setTradeCurrency)
    {
        if (setTradeCurrency != null)
        {
        }
    }

    [WorldPacketHandler(ClientOpcodes.SetTradeGold)]
    private void HandleSetTradeGold(SetTradeGold setTradeGold)
    {
        var myTrade = _session.Player.TradeData;

        if (myTrade == null)
            return;

        myTrade.UpdateClientStateIndex();
        myTrade.SetMoney(setTradeGold.Coinage);
    }

    [WorldPacketHandler(ClientOpcodes.SetTradeItem)]
    private void HandleSetTradeItem(SetTradeItem setTradeItem)
    {
        var myTrade = _session.Player.TradeData;

        if (myTrade == null)
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
        var item = _session.Player.GetItemByPos(setTradeItem.PackSlot, setTradeItem.ItemSlotInPack);

        if (item == null || (setTradeItem.TradeSlot != (byte)TradeSlots.NonTraded && !item.CanBeTraded(false, true)))
        {
            info.Status = TradeStatus.Cancelled;
            SendTradeStatus(info);

            return;
        }

        var iGUID = item.GUID;

        // prevent place single item into many trade slots using cheating and client bugs
        if (myTrade.HasItem(iGUID))
        {
            // cheating attempt
            info.Status = TradeStatus.Cancelled;
            SendTradeStatus(info);

            return;
        }

        myTrade.UpdateClientStateIndex();

        if (setTradeItem.TradeSlot != (byte)TradeSlots.NonTraded && item.IsBindedNotWith(myTrade.Trader))
        {
            info.Status = TradeStatus.NotOnTaplist;
            info.TradeSlot = setTradeItem.TradeSlot;
            SendTradeStatus(info);

            return;
        }

        myTrade.SetItem((TradeSlots)setTradeItem.TradeSlot, item);
    }

    [WorldPacketHandler(ClientOpcodes.UnacceptTrade)]
    private void HandleUnacceptTrade(UnacceptTrade packet)
    {
        if (packet == null)
            return;

        _session.Player.TradeData?.SetAccepted(false, true);
    }

    private void MoveItems(Item[] myItems, Item[] hisItems)
    {
        var trader = _session.Player.Trader;

        if (trader == null)
            return;

        for (byte i = 0; i < (int)TradeSlots.TradedCount; ++i)
        {
            List<ItemPosCount> traderDst = new();
            List<ItemPosCount> playerDst = new();
            var traderCanTrade = myItems[i] == null || trader.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, traderDst, myItems[i]) == InventoryResult.Ok;
            var playerCanTrade = hisItems[i] == null || _session.Player.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, playerDst, hisItems[i]) == InventoryResult.Ok;

            if (traderCanTrade && playerCanTrade)
            {
                // Ok, if trade item exists and can be stored
                // If we trade in both directions we had to check, if the trade will work before we actually do it
                // A roll back is not possible after we stored it
                if (myItems[i] != null)
                {
                    // logging
                    Log.Logger.Debug("partner storing: {0}", myItems[i].GUID.ToString());

                    if (_session.HasPermission(RBACPermissions.LogGmTrade))
                        Log.Logger.ForContext<GMCommands>().Information(
                                                                        "GM {0} (Account: {1}) trade: {2} (Entry: {3} Count: {4}) to player: {5} (Account: {6})",
                                                                        _session.Player.GetName(),
                                                                        _session.Player.Session.AccountId,
                                                                        myItems[i].Template.GetName(),
                                                                        myItems[i].Entry,
                                                                        myItems[i].Count,
                                                                        trader.GetName(),
                                                                        trader.Session.AccountId);

                    // adjust time (depends on /played)
                    if (myItems[i].IsBopTradeable)
                        myItems[i].SetCreatePlayedTime(trader.TotalPlayedTime - (_session.Player.TotalPlayedTime - myItems[i].ItemData.CreatePlayedTime));

                    // store
                    trader.MoveItemToInventory(traderDst, myItems[i], true, true);
                }

                if (hisItems[i] == null)
                    continue;

                // logging
                Log.Logger.Debug("player storing: {0}", hisItems[i].GUID.ToString());

                if (_session.HasPermission(RBACPermissions.LogGmTrade))
                    Log.Logger.ForContext<GMCommands>().Information(
                                   "GM {0} (Account: {1}) trade: {2} (Entry: {3} Count: {4}) to player: {5} (Account: {6})",
                                   trader.GetName(),
                                   trader.Session.AccountId,
                                   hisItems[i].Template.GetName(),
                                   hisItems[i].Entry,
                                   hisItems[i].Count,
                                   _session.Player.GetName(),
                                   _session.Player.Session.AccountId);

                // adjust time (depends on /played)
                if (hisItems[i].IsBopTradeable)
                    hisItems[i].SetCreatePlayedTime(_session.Player.TotalPlayedTime - (trader.TotalPlayedTime - hisItems[i].ItemData.CreatePlayedTime));

                // store
                _session.Player.MoveItemToInventory(playerDst, hisItems[i], true, true);
            }
            else
            {
                // in case of fatal error log error message
                // return the already removed items to the original owner
                if (myItems[i] != null)
                {
                    if (!traderCanTrade)
                        Log.Logger.Error("trader can't store item: {0}", myItems[i].GUID.ToString());

                    if (_session.Player.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, playerDst, myItems[i]) == InventoryResult.Ok)
                        _session.Player.MoveItemToInventory(playerDst, myItems[i], true, true);
                    else
                        Log.Logger.Error("player can't take item back: {0}", myItems[i].GUID.ToString());
                }

                // return the already removed items to the original owner
                if (hisItems[i] == null)
                    continue;

                if (!playerCanTrade)
                    Log.Logger.Error("player can't store item: {0}", hisItems[i].GUID.ToString());

                if (trader.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, traderDst, hisItems[i]) == InventoryResult.Ok)
                    trader.MoveItemToInventory(traderDst, hisItems[i], true, true);
                else
                    Log.Logger.Error("trader can't take item back: {0}", hisItems[i].GUID.ToString());
            }
        }
    }
}