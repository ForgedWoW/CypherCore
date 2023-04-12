// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage.Structs.I;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps;
using Forged.MapServer.Networking.Packets.Item;
using Forged.MapServer.Networking.Packets.Loot;
using Framework.Constants;

namespace Forged.MapServer.LootManagement;

public class LootRoll
{
    private static readonly TimeSpan LootRollTimeout = TimeSpan.FromMinutes(1);
    private readonly LootFactory _lootFactory;
    private readonly ObjectAccessor _objectAccessor;
    private readonly GameObjectManager _objectManager;
    private readonly Dictionary<ObjectGuid, PlayerRollVote> _rollVoteMap = new();

    private DateTime _endTime = DateTime.MinValue;
    private bool _isStarted;
    private Loot _loot;
    private LootItem _lootItem;
    private Map _map;
    private RollMask _voteMask;

    public LootRoll(GameObjectManager objectManager, ObjectAccessor objectAccessor, LootFactory lootFactory)
    {
        _objectManager = objectManager;
        _objectAccessor = objectAccessor;
        _lootFactory = lootFactory;
    }

    ~LootRoll()
    {
        if (_isStarted)
            SendAllPassed();

        foreach (var (playerGuid, roll) in _rollVoteMap)
        {
            if (roll.Vote != RollVote.NotEmitedYet)
                continue;

            _objectAccessor.GetPlayer(_map, playerGuid)?.RemoveLootRoll(this);
        }
    }

    public bool IsLootItem(ObjectGuid lootObject, uint lootListId)
    {
        return _loot.Guid == lootObject && _lootItem.LootListId == lootListId;
    }

    // Add vote from playerGuid
    public bool PlayerVote(Player player, RollVote vote)
    {
        var playerGuid = player.GUID;

        if (!_rollVoteMap.TryGetValue(playerGuid, out var voter))
            return false;

        voter.Vote = vote;

        if (vote != RollVote.Pass && vote != RollVote.NotValid)
            voter.RollNumber = (byte)RandomHelper.URand(1, 100);

        switch (vote)
        {
            case RollVote.Pass: // Player choose pass
            {
                SendRoll(playerGuid, -1, RollVote.Pass, null);

                break;
            }
            case RollVote.Need: // player choose Need
            {
                SendRoll(playerGuid, 0, RollVote.Need, null);
                player.UpdateCriteria(CriteriaType.RollAnyNeed, 1);

                break;
            }
            case RollVote.Greed: // player choose Greed
            {
                SendRoll(playerGuid, -1, RollVote.Greed, null);
                player.UpdateCriteria(CriteriaType.RollAnyGreed, 1);

                break;
            }
            case RollVote.Disenchant: // player choose Disenchant
            {
                SendRoll(playerGuid, -1, RollVote.Disenchant, null);
                player.UpdateCriteria(CriteriaType.RollAnyGreed, 1);

                break;
            }
            default: // Roll removed case
                return false;
        }

        return true;
    }

    // Try to start the group roll for the specified item (it may fail for quest item or any condition
    // If this method return false the roll have to be removed from the container to avoid any problem
    public bool TryToStart(Map map, Loot loot, uint lootListId, ushort enchantingSkill)
    {
        if (_isStarted)
            return false;

        if (lootListId >= loot.Items.Count)
            return false;

        _map = map;

        // initialize the data needed for the roll
        _lootItem = loot.Items[(int)lootListId];

        _loot = loot;
        _lootItem.IsBlocked = true; // block the item while rolling

        uint playerCount = 0;

        foreach (var allowedLooter in _lootItem.GetAllowedLooters())
        {
            var plr = _objectAccessor.GetPlayer(_map, allowedLooter);

            if (!_rollVoteMap.TryGetValue(allowedLooter, out var voter))
            {
                voter = new PlayerRollVote();
                _rollVoteMap.Add(allowedLooter, voter);
            }

            if (plr == null || !_lootItem.HasAllowedLooter(plr.GUID)) // check if player meet the condition to be able to roll this item
            {
                voter.Vote = RollVote.NotValid;

                continue;
            }

            // initialize player vote map
            voter.Vote = plr.PassOnGroupLoot ? RollVote.Pass : RollVote.NotEmitedYet;

            if (!plr.PassOnGroupLoot)
                plr.AddLootRoll(this);

            ++playerCount;
        }

        // initialize item prototype and check enchant possibilities for this group
        var itemTemplate = _objectManager.GetItemTemplate(_lootItem.Itemid);
        _voteMask = RollMask.AllMask;

        if (itemTemplate.HasFlag(ItemFlags2.CanOnlyRollGreed))
            _voteMask = _voteMask & ~RollMask.Need;

        var disenchant = GetItemDisenchantLoot();

        if (disenchant == null || disenchant.SkillRequired > enchantingSkill)
            _voteMask = _voteMask & ~RollMask.Disenchant;

        if (playerCount > 1) // check if more than one player can loot this item
        {
            // start the roll
            SendStartRoll();
            _endTime = GameTime.Now + LootRollTimeout;
            _isStarted = true;

            return true;
        }

        // no need to start roll if one or less player can loot this item so place it under threshold
        _lootItem.IsUnderthreshold = true;
        _lootItem.IsBlocked = false;

        return false;
    }

    // check if we can found a winner for this roll or if timer is expired
    public bool UpdateRoll()
    {
        KeyValuePair<ObjectGuid, PlayerRollVote> winner = default;

        if (AllPlayerVoted(ref winner) || _endTime <= GameTime.Now)
        {
            Finish(winner);

            return true;
        }

        return false;
    }

    private bool AllPlayerVoted(ref KeyValuePair<ObjectGuid, PlayerRollVote> winnerPair)
    {
        uint notVoted = 0;
        var isSomeoneNeed = false;

        foreach (var pair in _rollVoteMap)
            switch (pair.Value.Vote)
            {
                case RollVote.Need:
                    if (!isSomeoneNeed || winnerPair.Value == null || pair.Value.RollNumber > winnerPair.Value.RollNumber)
                    {
                        isSomeoneNeed = true; // first passage will force to set winner because need is prioritized
                        winnerPair = pair;
                    }

                    break;
                case RollVote.Greed:
                case RollVote.Disenchant:
                    if (!isSomeoneNeed) // if at least one need is detected then winner can't be a greed
                        if (winnerPair.Value == null || pair.Value.RollNumber > winnerPair.Value.RollNumber)
                            winnerPair = pair;

                    break;
                // Explicitly passing excludes a player from winning loot, so no action required.
                case RollVote.Pass:
                    break;
                case RollVote.NotEmitedYet:
                    ++notVoted;

                    break;
            }

        return notVoted == 0;
    }

    private void FillPacket(LootItemData lootItem)
    {
        lootItem.Quantity = _lootItem.Count;
        lootItem.LootListID = (byte)_lootItem.LootListId;
        lootItem.CanTradeToTapList = _lootItem.AllowedGuiDs.Count > 1;
        lootItem.Loot = new ItemInstance(_lootItem);
    }

    // terminate the roll
    private void Finish(KeyValuePair<ObjectGuid, PlayerRollVote> winnerPair)
    {
        _lootItem.IsBlocked = false;

        if (winnerPair.Value == null)
        {
            SendAllPassed();
        }
        else
        {
            _lootItem.RollWinnerGuid = winnerPair.Key;

            SendLootRollWon(winnerPair.Key, winnerPair.Value.RollNumber, winnerPair.Value.Vote);

            var player = _objectAccessor.FindConnectedPlayer(winnerPair.Key);

            if (player != null)
            {
                if (winnerPair.Value.Vote == RollVote.Need)
                    player.UpdateCriteria(CriteriaType.RollNeed, _lootItem.Itemid, winnerPair.Value.RollNumber);
                else if (winnerPair.Value.Vote == RollVote.Disenchant)
                    player.UpdateCriteria(CriteriaType.CastSpell, 13262);
                else
                    player.UpdateCriteria(CriteriaType.RollGreed, _lootItem.Itemid, winnerPair.Value.RollNumber);

                if (winnerPair.Value.Vote == RollVote.Disenchant)
                {
                    var disenchant = GetItemDisenchantLoot();
                    var loot = _lootFactory.GenerateLoot(_map, _loot.OwnerGuid, LootType.Disenchanting, disenchant.Id, LootStorageType.Disenchant, player, true);

                    if (!loot.AutoStore(player, ItemConst.NullBag, ItemConst.NullSlot, true))
                        for (uint i = 0; i < loot.Items.Count; ++i)
                        {
                            var disenchantLoot = loot.LootItemInSlot(i, player);

                            if (disenchantLoot != null)
                                player.SendItemRetrievalMail(disenchantLoot.Itemid, disenchantLoot.Count, disenchantLoot.Context);
                        }
                    else
                        _loot.NotifyItemRemoved((byte)_lootItem.LootListId, _map);
                }
                else
                {
                    player.StoreLootItem(_loot.OwnerGuid, (byte)_lootItem.LootListId, _loot);
                }
            }
        }

        _isStarted = false;
    }

    private ItemDisenchantLootRecord GetItemDisenchantLoot()
    {
        ItemInstance itemInstance = new(_lootItem);

        BonusData bonusData = new(itemInstance);

        if (!bonusData.CanDisenchant)
            return null;

        var itemTemplate = _objectManager.GetItemTemplate(_lootItem.Itemid);
        var itemLevel = Item.GetItemLevel(itemTemplate, bonusData, 1, 0, 0, 0, 0, false, 0);

        return Item.GetDisenchantLoot(itemTemplate, (uint)bonusData.Quality, itemLevel);
    }

    // Send all passed message
    private void SendAllPassed()
    {
        LootAllPassed lootAllPassed = new()
        {
            LootObj = _loot.Guid
        };

        FillPacket(lootAllPassed.Item);
        lootAllPassed.Item.UIType = LootSlotType.AllowLoot;
        lootAllPassed.Write();

        foreach (var (playerGuid, roll) in _rollVoteMap)
        {
            if (roll.Vote != RollVote.NotValid)
                continue;

            var player = _objectAccessor.GetPlayer(_map, playerGuid);

            player?.SendPacket(lootAllPassed);
        }
    }

    // Send roll 'value' of the whole group and the winner to the whole group
    private void SendLootRollWon(ObjectGuid targetGuid, int rollNumber, RollVote rollType)
    {
        // Send roll values
        foreach (var (playerGuid, roll) in _rollVoteMap)
            switch (roll.Vote)
            {
                case RollVote.Pass:
                    break;
                case RollVote.NotEmitedYet:
                case RollVote.NotValid:
                    SendRoll(playerGuid, 0, RollVote.Pass, targetGuid);

                    break;
                default:
                    SendRoll(playerGuid, roll.RollNumber, roll.Vote, targetGuid);

                    break;
            }

        LootRollWon lootRollWon = new()
        {
            LootObj = _loot.Guid,
            Winner = targetGuid,
            Roll = rollNumber,
            RollType = rollType
        };

        FillPacket(lootRollWon.Item);
        lootRollWon.Item.UIType = LootSlotType.Locked;
        lootRollWon.MainSpec = true; // offspec rolls not implemented
        lootRollWon.Write();

        foreach (var (playerGuid, roll) in _rollVoteMap)
        {
            if (roll.Vote == RollVote.NotValid)
                continue;

            if (playerGuid == targetGuid)
                continue;

            var player1 = _objectAccessor.GetPlayer(_map, playerGuid);

            player1?.SendPacket(lootRollWon);
        }

        var player = _objectAccessor.GetPlayer(_map, targetGuid);

        if (player == null)
            return;

        lootRollWon.Item.UIType = LootSlotType.AllowLoot;
        lootRollWon.Clear();
        player.SendPacket(lootRollWon);
    }

    // Send roll of targetGuid to the whole group (included targuetGuid)
    private void SendRoll(ObjectGuid targetGuid, int rollNumber, RollVote rollType, ObjectGuid? rollWinner)
    {
        LootRollBroadcast lootRoll = new()
        {
            LootObj = _loot.Guid,
            Player = targetGuid,
            Roll = rollNumber,
            RollType = rollType,
            Autopassed = false
        };

        FillPacket(lootRoll.Item);
        lootRoll.Item.UIType = LootSlotType.RollOngoing;
        lootRoll.Write();

        foreach (var (playerGuid, roll) in _rollVoteMap)
        {
            if (roll.Vote == RollVote.NotValid)
                continue;

            if (playerGuid == rollWinner)
                continue;

            var player = _objectAccessor.GetPlayer(_map, playerGuid);

            player?.SendPacket(lootRoll);
        }

        if (!rollWinner.HasValue)
            return;

        {
            var player = _objectAccessor.GetPlayer(_map, rollWinner.Value);

            if (player == null)
                return;

            lootRoll.Item.UIType = LootSlotType.AllowLoot;
            lootRoll.Clear();
            player.SendPacket(lootRoll);
        }
    }

    // Send the roll for the whole group
    private void SendStartRoll()
    {
        var itemTemplate = _objectManager.GetItemTemplate(_lootItem.Itemid);

        foreach (var (playerGuid, roll) in _rollVoteMap)
        {
            if (roll.Vote != RollVote.NotEmitedYet)
                continue;

            var player = _objectAccessor.GetPlayer(_map, playerGuid);

            if (player == null)
                continue;

            StartLootRoll startLootRoll = new()
            {
                LootObj = _loot.Guid,
                MapID = (int)_map.Id,
                RollTime = (uint)LootRollTimeout.TotalMilliseconds,
                Method = _loot.LootMethod,
                ValidRolls = _voteMask
            };

            // In NEED_BEFORE_GREED need disabled for non-usable item for player
            if (_loot.LootMethod == LootMethod.NeedBeforeGreed && player.CanRollNeedForItem(itemTemplate, _map, true) != InventoryResult.Ok)
                startLootRoll.ValidRolls &= ~RollMask.Need;

            FillPacket(startLootRoll.Item);
            startLootRoll.Item.UIType = LootSlotType.RollOngoing;

            player.SendPacket(startLootRoll);
        }

        // Handle auto pass option
        foreach (var (playerGuid, roll) in _rollVoteMap)
        {
            if (roll.Vote != RollVote.Pass)
                continue;

            SendRoll(playerGuid, -1, RollVote.Pass, null);
        }
    }
}