// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.BattleGrounds;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.C;
using Forged.MapServer.DataStorage.Structs.F;
using Forged.MapServer.DataStorage.Structs.P;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Globals.Caching;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Item;
using Forged.MapServer.Networking.Packets.NPC;
using Forged.MapServer.Networking.Packets.Pet;
using Forged.MapServer.Server;
using Framework.Constants;
using Framework.Database;
using Game.Common;
using Game.Common.Handlers;
using Serilog;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class NPCHandler : IWorldSessionHandler
{
    private readonly BattlegroundManager _battlegroundManager;
    private readonly CharacterDatabase _characterDatabase;
    private readonly ItemTemplateCache _itemTemplateCache;
    private readonly ConditionManager _conditionManager;
    private readonly DB6Storage<CurrencyTypesRecord> _currencyTypesRecords;
    private readonly DB6Storage<FactionTemplateRecord> _factionTemplateRecords;
    private readonly GameObjectManager _objectManager;
    private readonly PetHandler _petHandler;
    private readonly DB6Storage<PlayerConditionRecord> _playerConditionRecords;
    private readonly WorldSession _session;

    public NPCHandler(WorldSession session, DB6Storage<PlayerConditionRecord> playerConditionRecords, ConditionManager conditionManager, GameObjectManager objectManager,
                      DB6Storage<CurrencyTypesRecord> currencyTypesRecords, DB6Storage<FactionTemplateRecord> factionTemplateRecords, BattlegroundManager battlegroundManager,
                      CharacterDatabase characterDatabase, ClassFactory classFactory, ItemTemplateCache itemTemplateCache)
    {
        _session = session;
        _playerConditionRecords = playerConditionRecords;
        _conditionManager = conditionManager;
        _objectManager = objectManager;
        _currencyTypesRecords = currencyTypesRecords;
        _factionTemplateRecords = factionTemplateRecords;
        _battlegroundManager = battlegroundManager;
        _petHandler = classFactory.ResolveWithPositionalParameters<PetHandler>(session);
        _characterDatabase = characterDatabase;
        _itemTemplateCache = itemTemplateCache;
    }

    public void SendListInventory(ObjectGuid vendorGuid)
    {
        var vendor = _session.Player.GetNPCIfCanInteractWith(vendorGuid, NPCFlags.Vendor, NPCFlags2.None);

        if (vendor == null)
        {
            Log.Logger.Debug("WORLD: SendListInventory - {0} not found or you can not interact with him.", vendorGuid.ToString());
            _session.Player.SendSellError(SellResult.CantFindVendor, null, ObjectGuid.Empty);

            return;
        }

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        // Stop the npc if moving
        var pause = vendor.MovementTemplate.InteractionPauseTimer;

        if (pause != 0)
            vendor.PauseMovement(pause);

        vendor.HomePosition = vendor.Location;

        var rawItemCount = vendor.VendorItems?.ItemCount ?? 0;

        VendorInventory packet = new()
        {
            Vendor = vendor.GUID
        };

        var discountMod = _session.Player.GetReputationPriceDiscount(vendor);
        byte count = 0;

        for (uint slot = 0; slot < rawItemCount; ++slot)
        {
            var vendorItem = vendor.VendorItems?.GetItem(slot);

            if (vendorItem == null)
                continue;

            VendorItemPkt item = new();

            if (_playerConditionRecords.TryGetValue(vendorItem.PlayerConditionId, out var playerCondition))
                if (!_conditionManager.IsPlayerMeetingCondition(_session.Player, playerCondition))
                    item.PlayerConditionFailed = (int)playerCondition.Id;

            switch (vendorItem.Type)
            {
                case ItemVendorType.Item:
                {
                    var itemTemplate = _itemTemplateCache.GetItemTemplate(vendorItem.Item);

                    if (itemTemplate == null)
                        continue;

                    var leftInStock = vendorItem.Maxcount == 0 ? -1 : (int)vendor.GetVendorItemCurrentCount(vendorItem);

                    if (!_session.Player.IsGameMaster)
                    {
                        if (!Convert.ToBoolean(itemTemplate.AllowableClass & _session.Player.ClassMask) && itemTemplate.Bonding == ItemBondingType.OnAcquire)
                            continue;

                        if ((itemTemplate.HasFlag(ItemFlags2.FactionHorde) && _session.Player.Team == TeamFaction.Alliance) ||
                            (itemTemplate.HasFlag(ItemFlags2.FactionAlliance) && _session.Player.Team == TeamFaction.Horde))
                            continue;

                        if (leftInStock == 0)
                            continue;
                    }

                    if (!_conditionManager.IsObjectMeetingVendorItemConditions(vendor.Entry, vendorItem.Item, _session.Player, vendor))
                    {
                        Log.Logger.Debug("SendListInventory: conditions not met for creature entry {0} item {1}", vendor.Entry, vendorItem.Item);

                        continue;
                    }

                    var price = (ulong)Math.Floor(itemTemplate.BuyPrice * discountMod);
                    price = itemTemplate.BuyPrice > 0 ? Math.Max(1ul, price) : price;

                    var priceMod = _session.Player.GetTotalAuraModifier(AuraType.ModVendorItemsPrices);

                    if (priceMod != 0)
                        price -= MathFunctions.CalculatePct(price, priceMod);

                    item.MuID = (int)slot + 1;
                    item.Durability = (int)itemTemplate.MaxDurability;
                    item.ExtendedCostID = (int)vendorItem.ExtendedCost;
                    item.Type = (int)vendorItem.Type;
                    item.Quantity = leftInStock;
                    item.StackCount = (int)itemTemplate.BuyCount;
                    item.Price = price;
                    item.DoNotFilterOnVendor = vendorItem.IgnoreFiltering;
                    item.Refundable = itemTemplate.HasFlag(ItemFlags.ItemPurchaseRecord) && vendorItem.ExtendedCost != 0 && itemTemplate.MaxStackSize == 1;

                    item.Item.ItemID = vendorItem.Item;

                    if (!vendorItem.BonusListIDs.Empty())
                    {
                        item.Item.ItemBonus = new ItemBonuses
                        {
                            BonusListIDs = vendorItem.BonusListIDs
                        };
                    }

                    packet.Items.Add(item);

                    break;
                }
                case ItemVendorType.Currency when !_currencyTypesRecords.ContainsKey(vendorItem.Item):
                // there's no price defined for currencies, only extendedcost is used
                case ItemVendorType.Currency when vendorItem.ExtendedCost == 0:
                    continue;
                case ItemVendorType.Currency:
                    item.MuID = (int)slot + 1; // client expects counting to start at 1
                    item.ExtendedCostID = (int)vendorItem.ExtendedCost;
                    item.Item.ItemID = vendorItem.Item;
                    item.Type = (int)vendorItem.Type;
                    item.StackCount = (int)vendorItem.Maxcount;
                    item.DoNotFilterOnVendor = vendorItem.IgnoreFiltering;

                    packet.Items.Add(item);

                    break;

                default:
                    continue;
            }

            if (++count >= SharedConst.MaxVendorItems)
                break;
        }

        packet.Reason = (byte)(count != 0 ? VendorInventoryReason.None : VendorInventoryReason.Empty);

        _session.SendPacket(packet);
    }
    
    public void SendTabardVendorActivate(ObjectGuid guid)
    {
        NPCInteractionOpenResult npcInteraction = new()
        {
            Npc = guid,
            InteractionType = PlayerInteractionType.TabardVendor,
            Success = true
        };

        _session.SendPacket(npcInteraction);
    }

    public void SendTrainerList(Creature npc, uint trainerId)
    {
        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        var trainer = _objectManager.TrainerCache.GetTrainer(trainerId);

        if (trainer == null)
        {
            Log.Logger.Debug($"WORLD: SendTrainerList - trainer spells not found for trainer {npc.GUID} id {trainerId}");

            return;
        }

        _session.Player.PlayerTalkClass.InteractionData.Reset();
        _session.Player.PlayerTalkClass.InteractionData.SourceGuid = npc.GUID;
        _session.Player.PlayerTalkClass.InteractionData.TrainerId = trainerId;
        trainer.SendSpells(npc, _session.Player, _session.SessionDbLocaleIndex);
    }

    [WorldPacketHandler(ClientOpcodes.BinderActivate, Processing = PacketProcessing.Inplace)]
    private void HandleBinderActivate(Hello packet)
    {
        if (!_session.Player.Location.IsInWorld || !_session.Player.IsAlive)
            return;

        var unit = _session.Player.GetNPCIfCanInteractWith(packet.Unit, NPCFlags.Innkeeper, NPCFlags2.None);

        if (unit == null)
        {
            Log.Logger.Debug("WORLD: HandleBinderActivate - {0} not found or you can not interact with him.", packet.Unit.ToString());

            return;
        }

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        SendBindPoint(unit);
    }

    [WorldPacketHandler(ClientOpcodes.TalkToGossip, Processing = PacketProcessing.Inplace)]
    private void HandleGossipHello(Hello packet)
    {
        var unit = _session.Player.GetNPCIfCanInteractWith(packet.Unit, NPCFlags.Gossip, NPCFlags2.None);

        if (unit == null)
        {
            Log.Logger.Debug("WORLD: HandleGossipHello - {0} not found or you can not interact with him.", packet.Unit.ToString());

            return;
        }

        // set faction visible if needed
        if (_factionTemplateRecords.TryGetValue(unit.Faction, out var factionTemplateEntry))
            _session.Player.ReputationMgr.SetVisible(factionTemplateEntry);

        _session.Player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Interacting);

        // Stop the npc if moving
        var pause = unit.MovementTemplate.InteractionPauseTimer;

        if (pause != 0)
            unit.PauseMovement(pause);

        unit.HomePosition = unit.Location;

        // If spiritguide, no need for gossip menu, just put player into resurrect queue
        if (unit.IsSpiritGuide)
        {
            var bg = _session.Player.Battleground;

            if (bg != null)
            {
                bg.AddPlayerToResurrectQueue(unit.GUID, _session.Player.GUID);
                _battlegroundManager.SendAreaSpiritHealerQuery(_session.Player, bg, unit.GUID);

                return;
            }
        }

        _session.Player.PlayerTalkClass.ClearMenus();

        if (unit.AI.OnGossipHello(_session.Player))
            return;

        _session.Player.PrepareGossipMenu(unit, unit.Template.GossipMenuId, true);
        _session.Player.SendPreparedGossip(unit);
    }

    [WorldPacketHandler(ClientOpcodes.GossipSelectOption)]
    private void HandleGossipSelectOption(GossipSelectOption packet)
    {
        var gossipMenuItem = _session.Player.PlayerTalkClass.GossipMenu.GetItem(packet.GossipOptionID);

        if (gossipMenuItem == null)
            return;

        // Prevent cheating on C# scripted menus
        if (_session.Player.PlayerTalkClass.InteractionData.SourceGuid != packet.GossipUnit)
            return;

        Creature unit = null;
        GameObject go = null;

        if (packet.GossipUnit.IsCreatureOrVehicle)
        {
            unit = _session.Player.GetNPCIfCanInteractWith(packet.GossipUnit, NPCFlags.Gossip, NPCFlags2.None);

            if (unit == null)
            {
                Log.Logger.Debug("WORLD: HandleGossipSelectOption - {0} not found or you can't interact with him.", packet.GossipUnit.ToString());

                return;
            }
        }
        else if (packet.GossipUnit.IsGameObject)
        {
            go = _session.Player.GetGameObjectIfCanInteractWith(packet.GossipUnit);

            if (go == null)
            {
                Log.Logger.Debug("WORLD: HandleGossipSelectOption - {0} not found or you can't interact with it.", packet.GossipUnit.ToString());

                return;
            }
        }
        else
        {
            Log.Logger.Debug("WORLD: HandleGossipSelectOption - unsupported {0}.", packet.GossipUnit.ToString());

            return;
        }

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        if ((unit != null && unit.GetScriptId() != unit.LastUsedScriptID) || (go != null && go.ScriptId != go.LastUsedScriptID))
        {
            Log.Logger.Debug("WORLD: HandleGossipSelectOption - Script reloaded while in use, ignoring and set new scipt id");

            if (unit != null)
                unit.LastUsedScriptID = unit.GetScriptId();

            if (go != null)
                go.LastUsedScriptID = go.ScriptId;

            _session.Player.PlayerTalkClass.SendCloseGossip();

            return;
        }

        if (!string.IsNullOrEmpty(packet.PromotionCode))
        {
            if (unit != null)
            {
                if (!unit.AI.OnGossipSelectCode(_session.Player, packet.GossipID, gossipMenuItem.OrderIndex, packet.PromotionCode))
                    _session.Player.OnGossipSelect(unit, packet.GossipOptionID, packet.GossipID);
            }
            else
            {
                if (!go.AI.OnGossipSelectCode(_session.Player, packet.GossipID, gossipMenuItem.OrderIndex, packet.PromotionCode))
                    _session.Player.OnGossipSelect(go, packet.GossipOptionID, packet.GossipID);
            }
        }
        else
        {
            if (unit != null)
            {
                if (!unit.AI.OnGossipSelect(_session.Player, packet.GossipID, gossipMenuItem.OrderIndex))
                    _session.Player.OnGossipSelect(unit, packet.GossipOptionID, packet.GossipID);
            }
            else
            {
                if (!go.AI.OnGossipSelect(_session.Player, packet.GossipID, gossipMenuItem.OrderIndex))
                    _session.Player.OnGossipSelect(go, packet.GossipOptionID, packet.GossipID);
            }
        }
    }

    [WorldPacketHandler(ClientOpcodes.ListInventory, Processing = PacketProcessing.Inplace)]
    private void HandleListInventory(Hello packet)
    {
        if (!_session.Player.IsAlive)
            return;

        SendListInventory(packet.Unit);
    }

    [WorldPacketHandler(ClientOpcodes.RepairItem, Processing = PacketProcessing.Inplace)]
    private void HandleRepairItem(RepairItem packet)
    {
        var unit = _session.Player.GetNPCIfCanInteractWith(packet.NpcGUID, NPCFlags.Repair, NPCFlags2.None);

        if (unit == null)
        {
            Log.Logger.Debug("WORLD: HandleRepairItemOpcode - {0} not found or you can not interact with him.", packet.NpcGUID.ToString());

            return;
        }

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        // reputation discount
        var discountMod = _session.Player.GetReputationPriceDiscount(unit);

        if (!packet.ItemGUID.IsEmpty)
        {
            Log.Logger.Debug("ITEM: Repair {0}, at {1}", packet.ItemGUID.ToString(), packet.NpcGUID.ToString());

            var item = _session.Player.GetItemByGuid(packet.ItemGUID);

            if (item != null)
                _session.Player.DurabilityRepair(item.Pos, true, discountMod);
        }
        else
        {
            Log.Logger.Debug("ITEM: Repair all items at {0}", packet.NpcGUID.ToString());
            _session.Player.DurabilityRepairAll(true, discountMod, packet.UseGuildBank);
        }
    }

    [WorldPacketHandler(ClientOpcodes.RequestStabledPets, Processing = PacketProcessing.Inplace)]
    private void HandleRequestStabledPets(RequestStabledPets packet)
    {
        if (!_petHandler.CheckStableMaster(packet.StableMaster))
            return;

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        // remove mounts this fix bug where getting pet from stable while mounted deletes pet.
        if (_session.Player.IsMounted)
            _session.Player.RemoveAurasByType(AuraType.Mounted);

        _session.Player.SetStableMaster(packet.StableMaster);
    }

    [WorldPacketHandler(ClientOpcodes.SetPetSlot)]
    private void HandleSetPetSlot(SetPetSlot setPetSlot)
    {
        if (!_petHandler.CheckStableMaster(setPetSlot.StableMaster) || setPetSlot.DestSlot >= (byte)PetSaveMode.LastStableSlot)
        {
            SendPetStableResult(StableResult.InternalError);

            return;
        }
        
        _session.Player.SetPetSlot(setPetSlot.PetNumber, (PetSaveMode)setPetSlot.DestSlot);
    }

    [WorldPacketHandler(ClientOpcodes.SpiritHealerActivate)]
    private void HandleSpiritHealerActivate(SpiritHealerActivate packet)
    {
        var unit = _session.Player.GetNPCIfCanInteractWith(packet.Healer, NPCFlags.SpiritHealer, NPCFlags2.None);

        if (unit == null)
        {
            Log.Logger.Debug("WORLD: HandleSpiritHealerActivateOpcode - {0} not found or you can not interact with him.", packet.Healer.ToString());

            return;
        }

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        SendSpiritResurrect();
    }

    [WorldPacketHandler(ClientOpcodes.TabardVendorActivate, Processing = PacketProcessing.Inplace)]
    private void HandleTabardVendorActivate(Hello packet)
    {
        var unit = _session.Player.GetNPCIfCanInteractWith(packet.Unit, NPCFlags.TabardDesigner, NPCFlags2.None);

        if (unit == null)
        {
            Log.Logger.Debug("WORLD: HandleTabardVendorActivateOpcode - {0} not found or you can not interact with him.", packet.Unit.ToString());

            return;
        }

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        SendTabardVendorActivate(packet.Unit);
    }

    [WorldPacketHandler(ClientOpcodes.TrainerBuySpell, Processing = PacketProcessing.Inplace)]
    private void HandleTrainerBuySpell(TrainerBuySpell packet)
    {
        var npc = _session.Player.GetNPCIfCanInteractWith(packet.TrainerGUID, NPCFlags.Trainer, NPCFlags2.None);

        if (npc == null)
        {
            Log.Logger.Debug($"WORLD: HandleTrainerBuySpell - {packet.TrainerGUID} not found or you can not interact with him.");

            return;
        }

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        if (_session.Player.PlayerTalkClass.InteractionData.SourceGuid != packet.TrainerGUID)
            return;

        if (_session.Player.PlayerTalkClass.InteractionData.TrainerId != packet.TrainerID)
            return;

        // check present spell in trainer spell list
        var trainer = _objectManager.TrainerCache.GetTrainer(packet.TrainerID);

        if (trainer == null)
            return;

        trainer.TeachSpell(npc, _session.Player, packet.SpellID);
    }

    [WorldPacketHandler(ClientOpcodes.TrainerList, Processing = PacketProcessing.Inplace)]
    private void HandleTrainerList(Hello packet)
    {
        var npc = _session.Player.GetNPCIfCanInteractWith(packet.Unit, NPCFlags.Trainer, NPCFlags2.None);

        if (npc == null)
        {
            Log.Logger.Debug($"WorldSession.SendTrainerList - {packet.Unit} not found or you can not interact with him.");

            return;
        }

        var trainerId = _objectManager.GetCreatureDefaultTrainer(npc.Entry);

        if (trainerId != 0)
            SendTrainerList(npc, trainerId);
        else
            Log.Logger.Debug($"WorldSession.SendTrainerList - Creature id {npc.Entry} has no trainer data.");
    }

    private void SendBindPoint(Creature npc)
    {
        // prevent set homebind to instances in any case
        if (_session.Player.Location.Map.Instanceable)
            return;

        uint bindspell = 3286;

        // send spell for homebinding (3286)
        npc.SpellFactory.CastSpell(_session.Player, bindspell, true);

        _session.Player.PlayerTalkClass.SendCloseGossip();
    }

    private void SendPetStableResult(StableResult result)
    {
        PetStableResult petStableResult = new()
        {
            Result = result
        };

        _session.SendPacket(petStableResult);
    }

    private void SendSpiritResurrect()
    {
        _session.Player.ResurrectPlayer(0.5f, true);

        _session.Player.DurabilityLossAll(0.25f, true);

        // get corpse nearest graveyard
        WorldSafeLocsEntry corpseGrave = null;
        var corpseLocation = _session.Player.CorpseLocation;

        if (_session.Player.HasCorpse)
            corpseGrave = _objectManager.GraveyardCache.GetClosestGraveYard(corpseLocation, _session.Player.Team, _session.Player);

        // now can spawn bones
        _session.Player.SpawnCorpseBones();

        // teleport to nearest from corpse graveyard, if different from nearest to player ghost
        if (corpseGrave != null)
        {
            var ghostGrave = _objectManager.GraveyardCache.GetClosestGraveYard(_session.Player.Location, _session.Player.Team, _session.Player);

            if (corpseGrave != ghostGrave)
                _session.Player.TeleportTo(corpseGrave.Location);
        }
    }

    private void SendTrainerBuyFailed(ObjectGuid trainerGUID, uint spellID, TrainerFailReason trainerFailedReason)
    {
        TrainerBuyFailed trainerBuyFailed = new()
        {
            TrainerGUID = trainerGUID,
            SpellID = spellID,                        // should be same as in packet from client
            TrainerFailedReason = trainerFailedReason // 1 == "Not enough money for trainer service." 0 == "Trainer service %d unavailable."
        };

        _session.SendPacket(trainerBuyFailed);
    }
}