// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.C;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Loot;
using Forged.MapServer.Server;
using Forged.MapServer.Spells;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class LootHandler : IWorldSessionHandler
{
    private readonly CellCalculator _cellCalculator;
    private readonly DB6Storage<ChrSpecializationRecord> _chrSpecializationRecords;
    private readonly LootItemStorage _lootItemStorage;
    private readonly ObjectAccessor _objectAccessor;
    private readonly WorldSession _session;
    private readonly UnitCombatHelpers _unitCombatHelpers;

    public LootHandler(WorldSession session, UnitCombatHelpers unitCombatHelpers, CellCalculator cellCalculator, ObjectAccessor objectAccessor,
                       DB6Storage<ChrSpecializationRecord> chrSpecializationRecords, LootItemStorage lootItemStorage)
    {
        _session = session;
        _unitCombatHelpers = unitCombatHelpers;
        _cellCalculator = cellCalculator;
        _objectAccessor = objectAccessor;
        _chrSpecializationRecords = chrSpecializationRecords;
        _lootItemStorage = lootItemStorage;
    }

    [WorldPacketHandler(ClientOpcodes.LootItem)]
    private void HandleAutostoreLootItem(LootItemPkt packet)
    {
        var aeResult = _session.Player.GetAELootView().Count > 1 ? new AELootResult() : null;

        // @todo Implement looting by LootObject guid
        foreach (var req in packet.Loot)
        {
            if (!_session.Player.GetAELootView().TryGetValue(req.Object, out var loot))
            {
                _session.Player.SendLootRelease(ObjectGuid.Empty);

                continue;
            }
            if (loot.OwnerGuid.IsGameObject)
            {
                var go = _session.Player.Location.Map.GetGameObject(loot.OwnerGuid);

                // not check distance for GO in case owned GO (fishing bobber case, for example) or Fishing hole GO
                if (go == null || (go.OwnerGUID != _session.Player.GUID && go.GoType != GameObjectTypes.FishingHole && !go.IsWithinDistInMap(_session.Player)))
                {
                    _session.Player.SendLootRelease(loot.OwnerGuid);

                    continue;
                }
            }
            else if (loot.OwnerGuid.IsCreatureOrVehicle)
            {
                var creature = _session.Player.Location.Map.GetCreature(loot.OwnerGuid);

                if (creature == null)
                {
                    _session.Player.SendLootError(req.Object, loot.OwnerGuid, LootError.NoLoot);

                    continue;
                }

                if (!creature.Location.IsWithinDistInMap(_session.Player, AELootCreatureCheck.LootDistance))
                {
                    _session.Player.SendLootError(req.Object, loot.OwnerGuid, LootError.TooFar);

                    continue;
                }
            }

            _session.Player.StoreLootItem(loot.OwnerGuid, req.LootListID, loot, aeResult);

            // If player is removing the last LootItem, delete the empty container.
            if (loot.IsLooted() && loot.OwnerGuid.IsItem)
                _session.Player.Session.DoLootRelease(loot);
        }

        if (aeResult != null)
            foreach (var resultValue in aeResult.ByOrder)
            {
                _session.Player.SendNewItem(resultValue.Item, resultValue.Count, false, false, true, resultValue.DungeonEncounterId);
                _session.Player.UpdateCriteria(CriteriaType.LootItem, resultValue.Item.Entry, resultValue.Count);
                _session.Player.UpdateCriteria(CriteriaType.GetLootByType, resultValue.Item.Entry, resultValue.Count, (ulong)resultValue.LootType);
                _session.Player.UpdateCriteria(CriteriaType.LootAnyItem, resultValue.Item.Entry, resultValue.Count);
            }

        _unitCombatHelpers.ProcSkillsAndAuras(_session.Player, null, new ProcFlagsInit(ProcFlags.Looted), new ProcFlagsInit(), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);
    }

    [WorldPacketHandler(ClientOpcodes.LootUnit)]
    private void HandleLoot(LootUnit packet)
    {
        // Check possible cheat
        if (!_session.Player.IsAlive || !packet.Unit.IsCreatureOrVehicle)
            return;

        var lootTarget = ObjectAccessor.GetCreature(_session.Player, packet.Unit);

        if (lootTarget == null)
            return;

        AELootCreatureCheck check = new(_session.Player, packet.Unit);

        if (!check.IsValidLootTarget(lootTarget))
            return;

        // interrupt cast
        if (_session.Player.IsNonMeleeSpellCast(false))
            _session.Player.InterruptNonMeleeSpells(false);

        _session.Player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Looting);

        List<Creature> corpses = new();
        CreatureListSearcher searcher = new(_session.Player, corpses, check, GridType.Grid);
        _cellCalculator.VisitGrid(_session.Player, searcher, AELootCreatureCheck.LootDistance);

        if (!corpses.Empty())
            _session.SendPacket(new AELootTargets((uint)corpses.Count + 1));

        _session.Player.SendLoot(lootTarget.GetLootForPlayer(_session.Player));

        if (corpses.Empty())
            return;

        // main target
        _session.SendPacket(new AELootTargetsAck());

        foreach (var creature in corpses)
        {
            _session.Player.SendLoot(creature.GetLootForPlayer(_session.Player), true);
            _session.SendPacket(new AELootTargetsAck());
        }
    }

    [WorldPacketHandler(ClientOpcodes.MasterLootItem)]
    private void HandleLootMasterGive(MasterLootItem masterLootItem)
    {
        AELootResult aeResult = new();

        if (_session.Player.Group == null || _session.Player.Group.LooterGuid != _session.Player.GUID)
        {
            _session.Player.SendLootError(ObjectGuid.Empty, ObjectGuid.Empty, LootError.DidntKill);

            return;
        }

        // player on other map
        var target = _objectAccessor.GetPlayer(_session.Player, masterLootItem.Target);

        if (target == null)
        {
            _session.Player.SendLootError(ObjectGuid.Empty, ObjectGuid.Empty, LootError.PlayerNotFound);

            return;
        }

        foreach (var req in masterLootItem.Loot)
        {
            var loot = _session.Player.GetAELootView().LookupByKey(req.Object);

            if (loot is not { LootMethod: LootMethod.MasterLoot })
                return;

            if (!_session.Player.IsInRaidWith(target) || !_session.Player.Location.IsInMap(target))
            {
                _session.Player.SendLootError(req.Object, loot.OwnerGuid, LootError.MasterOther);
                Log.Logger.Information($"MasterLootItem: Player {_session.Player.GetName()} tried to give an item to ineligible player {target.GetName()} !");

                return;
            }

            if (!loot.HasAllowedLooter(masterLootItem.Target))
            {
                _session.Player.SendLootError(req.Object, loot.OwnerGuid, LootError.MasterOther);

                return;
            }

            if (req.LootListID >= loot.Items.Count)
            {
                Log.Logger.Debug($"MasterLootItem: Player {_session.Player.GetName()} might be using a hack! (slot {req.LootListID}, size {loot.Items.Count})");

                return;
            }

            var item = loot.Items[req.LootListID];

            List<ItemPosCount> dest = new();
            var msg = target.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, item.Itemid, item.Count);

            if (!item.HasAllowedLooter(target.GUID))
                msg = InventoryResult.CantEquipEver;

            if (msg != InventoryResult.Ok)
            {
                if (msg == InventoryResult.ItemMaxCount)
                    _session.Player.SendLootError(req.Object, loot.OwnerGuid, LootError.MasterUniqueItem);
                else if (msg == InventoryResult.InvFull)
                    _session.Player.SendLootError(req.Object, loot.OwnerGuid, LootError.MasterInvFull);
                else
                    _session.Player.SendLootError(req.Object, loot.OwnerGuid, LootError.MasterOther);

                return;
            }

            // now move item from loot to target inventory
            var newitem = target.StoreNewItem(dest, item.Itemid, true, item.RandomBonusListId, item.GetAllowedLooters(), item.Context, item.BonusListIDs);
            aeResult.Add(newitem, item.Count, loot.LootType, loot.DungeonEncounterId);

            // mark as looted
            item.Count = 0;
            item.IsLooted = true;

            loot.NotifyItemRemoved(req.LootListID, _session.Player.Location.Map);
            --loot.UnlootedCount;
        }

        foreach (var resultValue in aeResult.ByOrder)
        {
            target.SendNewItem(resultValue.Item, resultValue.Count, false, false, true);
            target.UpdateCriteria(CriteriaType.LootItem, resultValue.Item.Entry, resultValue.Count);
            target.UpdateCriteria(CriteriaType.GetLootByType, resultValue.Item.Entry, resultValue.Count, (ulong)resultValue.LootType);
            target.UpdateCriteria(CriteriaType.LootAnyItem, resultValue.Item.Entry, resultValue.Count);
        }
    }

    [WorldPacketHandler(ClientOpcodes.LootMoney)]
    private void HandleLootMoney(LootMoney lootMoney)
    {
        if (lootMoney == null) return;

        foreach (var lootView in _session.Player.GetAELootView())
        {
            var loot = lootView.Value;
            var guid = loot.OwnerGuid;
            var shareMoney = loot.LootType == LootType.Corpse;

            loot.NotifyMoneyRemoved(_session.Player.Location.Map);

            if (shareMoney && _session.Player.Group != null) //item, pickpocket and players can be looted only single player
            {
                var group = _session.Player.Group;

                List<Player> playersNear = new();

                for (var refe = group.FirstMember; refe != null; refe = refe.Next())
                {
                    var member = refe.Source;

                    if (member == null)
                        continue;

                    if (!loot.HasAllowedLooter(member.GUID))
                        continue;

                    if (_session.Player.IsAtGroupRewardDistance(member))
                        playersNear.Add(member);
                }

                var goldPerPlayer = (ulong)(loot.Gold / playersNear.Count);

                foreach (var pl in playersNear)
                {
                    var goldMod = MathFunctions.CalculatePct(goldPerPlayer, pl.GetTotalAuraModifierByMiscValue(AuraType.ModMoneyGain, 1));

                    pl.ModifyMoney((long)(goldPerPlayer + goldMod));
                    pl.UpdateCriteria(CriteriaType.MoneyLootedFromCreatures, goldPerPlayer);

                    LootMoneyNotify packet = new()
                    {
                        Money = goldPerPlayer,
                        MoneyMod = goldMod,
                        SoleLooter = playersNear.Count <= 1
                    };

                    pl.SendPacket(packet);
                }
            }
            else
            {
                var goldMod = MathFunctions.CalculatePct(loot.Gold, _session.Player.GetTotalAuraModifierByMiscValue(AuraType.ModMoneyGain, 1));

                _session.Player.ModifyMoney(loot.Gold + goldMod);
                _session.Player.UpdateCriteria(CriteriaType.MoneyLootedFromCreatures, loot.Gold);

                LootMoneyNotify packet = new()
                {
                    Money = loot.Gold,
                    MoneyMod = goldMod,
                    SoleLooter = true // "You loot..."
                };

                _session.SendPacket(packet);
            }

            loot.Gold = 0;

            // Delete the money loot record from the DB
            if (loot.LootType == LootType.Item)
                _lootItemStorage.RemoveStoredMoneyForContainer(guid.Counter);

            // Delete container if empty
            if (loot.IsLooted() && guid.IsItem)
                _session.DoLootRelease(loot);
        }
    }

    [WorldPacketHandler(ClientOpcodes.LootRelease)]
    private void HandleLootRelease(LootRelease packet)
    {
        // cheaters can modify lguid to prevent correct apply loot release code and re-loot
        // use internal stored guid
        var loot = _session.Player.GetLootByWorldObjectGUID(packet.Unit);

        if (loot != null)
            _session.DoLootRelease(loot);
    }

    [WorldPacketHandler(ClientOpcodes.LootRoll)]
    private void HandleLootRoll(LootRollPacket packet)
    {
        var lootRoll = _session.Player.GetLootRoll(packet.LootObj, packet.LootListID);

        lootRoll?.PlayerVote(_session.Player, packet.RollType);
    }

    [WorldPacketHandler(ClientOpcodes.SetLootSpecialization)]
    private void HandleSetLootSpecialization(SetLootSpecialization packet)
    {
        if (packet.SpecID != 0)
        {
            if (!_chrSpecializationRecords.TryGetValue(packet.SpecID, out var chrSpec))
                return;

            if (chrSpec.ClassID == (uint)_session.Player.Class)
                _session.Player.SetLootSpecId(packet.SpecID);
        }
        else
            _session.Player.SetLootSpecId(0);
    }

    private class AELootCreatureCheck : ICheck<Creature>
    {
        public static readonly float LootDistance = 30.0f;

        private readonly Player _looter;
        private readonly ObjectGuid _mainLootTarget;

        public AELootCreatureCheck(Player looter, ObjectGuid mainLootTarget)
        {
            _looter = looter;
            _mainLootTarget = mainLootTarget;
        }

        public bool Invoke(Creature creature)
        {
            return IsValidAELootTarget(creature);
        }

        public bool IsValidLootTarget(Creature creature)
        {
            if (creature.IsAlive)
                return false;

            return _looter.Location.IsWithinDist(creature, LootDistance) && _looter.IsAllowedToLoot(creature);
        }

        private bool IsValidAELootTarget(Creature creature)
        {
            return creature.GUID != _mainLootTarget && IsValidLootTarget(creature);
        }
    }
}