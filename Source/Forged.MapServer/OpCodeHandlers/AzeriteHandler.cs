// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Azerite;
using Forged.MapServer.Networking.Packets.NPC;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common.Handlers;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class AzeriteHandler : IWorldSessionHandler
{
    private readonly AzeriteItemFactory _azeriteItem;
    private readonly DB6Storage<AzeriteItemMilestonePowerRecord> _azeriteItemMilestonePowerRecords;
    private readonly DB6Storage<AzeritePowerRecord> _azeritePowerRecords;
    private readonly DB2Manager _db2Manager;
    private readonly WorldSession _session;

    public AzeriteHandler(WorldSession session, AzeriteItemFactory azeriteItem, DB6Storage<AzeriteItemMilestonePowerRecord> azeriteItemMilestonePowerRecords,
                          DB2Manager db2Manager, DB6Storage<AzeritePowerRecord> azeritePowerRecords)
    {
        _session = session;
        _azeriteItem = azeriteItem;
        _azeriteItemMilestonePowerRecords = azeriteItemMilestonePowerRecords;
        _db2Manager = db2Manager;
        _azeritePowerRecords = azeritePowerRecords;
    }

    public void SendAzeriteRespecNPC(ObjectGuid npc)
    {
        _session.SendPacket(new NPCInteractionOpenResult()
        {
            Npc = npc,
            InteractionType = PlayerInteractionType.AzeriteRespec,
            Success = true
        });
    }

    [WorldPacketHandler(ClientOpcodes.AzeriteEmpoweredItemSelectPower, Processing = PacketProcessing.Inplace)]
    private void HandleAzeriteEmpoweredItemSelectPower(AzeriteEmpoweredItemSelectPower azeriteEmpoweredItemSelectPower)
    {
        var item = _session.Player.GetItemByPos(azeriteEmpoweredItemSelectPower.ContainerSlot, azeriteEmpoweredItemSelectPower.Slot);

        if (item == null)
            return;

        var azeritePower = _azeritePowerRecords.LookupByKey(azeriteEmpoweredItemSelectPower.AzeritePowerID);

        if (azeritePower == null)
            return;

        var azeriteEmpoweredItem = item.AsAzeriteEmpoweredItem;

        if (azeriteEmpoweredItem == null)
            return;

        // Validate tier
        var actualTier = azeriteEmpoweredItem.GetTierForAzeritePower(_session.Player.Class, azeriteEmpoweredItemSelectPower.AzeritePowerID);

        if (azeriteEmpoweredItemSelectPower.Tier > SharedConst.MaxAzeriteEmpoweredTier || azeriteEmpoweredItemSelectPower.Tier != actualTier)
            return;

        uint azeriteLevel = 0;
        var heartOfAzeroth = _session.Player.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);

        if (heartOfAzeroth == null)
            return;

        var azeriteItem = heartOfAzeroth.AsAzeriteItem;

        if (azeriteItem != null)
            azeriteLevel = azeriteItem.GetEffectiveLevel();

        // Check required heart of azeroth level
        if (azeriteLevel < azeriteEmpoweredItem.GetRequiredAzeriteLevelForTier((uint)actualTier))
            return;

        // tiers are ordered backwards, you first select the highest one
        for (var i = actualTier + 1; i < azeriteEmpoweredItem.GetMaxAzeritePowerTier(); ++i)
            if (azeriteEmpoweredItem.GetSelectedAzeritePower(i) == 0)
                return;

        var activateAzeritePower = azeriteEmpoweredItem.IsEquipped && heartOfAzeroth.IsEquipped;

        if (azeritePower.ItemBonusListID != 0 && activateAzeritePower)
            _session.Player._ApplyItemMods(azeriteEmpoweredItem, azeriteEmpoweredItem.Slot, false);

        azeriteEmpoweredItem.SetSelectedAzeritePower(actualTier, azeriteEmpoweredItemSelectPower.AzeritePowerID);

        if (activateAzeritePower)
        {
            // apply all item mods when azerite power grants a bonus, item level changes and that affects stats and auras that scale with item level
            if (azeritePower.ItemBonusListID != 0)
                _session.Player._ApplyItemMods(azeriteEmpoweredItem, azeriteEmpoweredItem.Slot, true);
            else
                _session.Player.ApplyAzeritePower(azeriteEmpoweredItem, azeritePower, true);
        }

        azeriteEmpoweredItem.SetState(ItemUpdateState.Changed, _session.Player);
    }

    [WorldPacketHandler(ClientOpcodes.AzeriteEmpoweredItemViewed, Processing = PacketProcessing.Inplace)]
    private void HandleAzeriteEmpoweredItemViewed(AzeriteEmpoweredItemViewed azeriteEmpoweredItemViewed)
    {
        var item = _session.Player.GetItemByGuid(azeriteEmpoweredItemViewed.ItemGUID);

        if (item == null || !item.IsAzeriteEmpoweredItem)
            return;

        item.SetItemFlag(ItemFieldFlags.AzeriteEmpoweredItemViewed);
        item.SetState(ItemUpdateState.Changed, _session.Player);
    }

    [WorldPacketHandler(ClientOpcodes.AzeriteEssenceActivateEssence, Processing = PacketProcessing.Inplace)]
    private void HandleAzeriteEssenceActivateEssence(AzeriteEssenceActivateEssence azeriteEssenceActivateEssence)
    {
        ActivateEssenceFailed activateEssenceResult = new()
        {
            AzeriteEssenceID = azeriteEssenceActivateEssence.AzeriteEssenceID
        };

        var item = _session.Player.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Equipment);

        if (item == null)
        {
            activateEssenceResult.Reason = AzeriteEssenceActivateResult.NotEquipped;
            activateEssenceResult.Slot = azeriteEssenceActivateEssence.Slot;
            _session.SendPacket(activateEssenceResult);

            return;
        }

        var azeriteItem = item.AsAzeriteItem;

        if (azeriteItem == null || !azeriteItem.CanUseEssences())
        {
            activateEssenceResult.Reason = AzeriteEssenceActivateResult.ConditionFailed;
            _session.SendPacket(activateEssenceResult);

            return;
        }

        if (azeriteEssenceActivateEssence.Slot >= SharedConst.MaxAzeriteEssenceSlot || !azeriteItem.HasUnlockedEssenceSlot(azeriteEssenceActivateEssence.Slot))
        {
            activateEssenceResult.Reason = AzeriteEssenceActivateResult.SlotLocked;
            _session.SendPacket(activateEssenceResult);

            return;
        }

        var selectedEssences = azeriteItem.GetSelectedAzeriteEssences();

        // essence is already in that slot, nothing to do
        if (selectedEssences != null && selectedEssences.AzeriteEssenceID[azeriteEssenceActivateEssence.Slot] == azeriteEssenceActivateEssence.AzeriteEssenceID)
            return;

        var rank = azeriteItem.GetEssenceRank(azeriteEssenceActivateEssence.AzeriteEssenceID);

        if (rank == 0)
        {
            activateEssenceResult.Reason = AzeriteEssenceActivateResult.EssenceNotUnlocked;
            activateEssenceResult.Arg = azeriteEssenceActivateEssence.AzeriteEssenceID;
            _session.SendPacket(activateEssenceResult);

            return;
        }

        if (_session.Player.IsInCombat)
        {
            activateEssenceResult.Reason = AzeriteEssenceActivateResult.AffectingCombat;
            activateEssenceResult.Slot = azeriteEssenceActivateEssence.Slot;
            _session.SendPacket(activateEssenceResult);

            return;
        }

        if (_session.Player.IsDead)
        {
            activateEssenceResult.Reason = AzeriteEssenceActivateResult.CantDoThatRightNow;
            activateEssenceResult.Slot = azeriteEssenceActivateEssence.Slot;
            _session.SendPacket(activateEssenceResult);

            return;
        }

        if (!_session.Player.HasPlayerFlag(PlayerFlags.Resting) && !_session.Player.HasUnitFlag2(UnitFlags2.AllowChangingTalents))
        {
            activateEssenceResult.Reason = AzeriteEssenceActivateResult.NotInRestArea;
            activateEssenceResult.Slot = azeriteEssenceActivateEssence.Slot;
            _session.SendPacket(activateEssenceResult);

            return;
        }

        if (selectedEssences != null)
        {
            // need to remove selected essence from another slot if selected
            var removeEssenceFromSlot = -1;

            for (var slot = 0; slot < SharedConst.MaxAzeriteEssenceSlot; ++slot)
                if (azeriteEssenceActivateEssence.Slot != slot && selectedEssences.AzeriteEssenceID[slot] == azeriteEssenceActivateEssence.AzeriteEssenceID)
                    removeEssenceFromSlot = slot;

            // check cooldown of major essence slot
            if (selectedEssences.AzeriteEssenceID[0] != 0 && (azeriteEssenceActivateEssence.Slot == 0 || removeEssenceFromSlot == 0))
                for (uint essenceRank = 1; essenceRank <= rank; ++essenceRank)
                {
                    var azeriteEssencePower = _db2Manager.GetAzeriteEssencePower(selectedEssences.AzeriteEssenceID[0], essenceRank);

                    if (_session.Player.SpellHistory.HasCooldown(azeriteEssencePower.MajorPowerDescription))
                    {
                        activateEssenceResult.Reason = AzeriteEssenceActivateResult.CantRemoveEssence;
                        activateEssenceResult.Arg = azeriteEssencePower.MajorPowerDescription;
                        activateEssenceResult.Slot = azeriteEssenceActivateEssence.Slot;
                        _session.SendPacket(activateEssenceResult);

                        return;
                    }
                }

            if (removeEssenceFromSlot != -1)
            {
                _session.Player.ApplyAzeriteEssence(azeriteItem,
                                            selectedEssences.AzeriteEssenceID[removeEssenceFromSlot],
                                            SharedConst.MaxAzeriteEssenceRank,
                                            (AzeriteItemMilestoneType)_db2Manager.GetAzeriteItemMilestonePower(removeEssenceFromSlot).Type == AzeriteItemMilestoneType.MajorEssence,
                                            false);

                azeriteItem.SetSelectedAzeriteEssence(removeEssenceFromSlot, 0);
            }

            if (selectedEssences.AzeriteEssenceID[azeriteEssenceActivateEssence.Slot] != 0)
                _session.Player.ApplyAzeriteEssence(azeriteItem,
                                            selectedEssences.AzeriteEssenceID[azeriteEssenceActivateEssence.Slot],
                                            SharedConst.MaxAzeriteEssenceRank,
                                            (AzeriteItemMilestoneType)_db2Manager.GetAzeriteItemMilestonePower(azeriteEssenceActivateEssence.Slot).Type == AzeriteItemMilestoneType.MajorEssence,
                                            false);
        }
        else
        {
            azeriteItem.CreateSelectedAzeriteEssences(_session.Player.GetPrimarySpecialization());
        }

        azeriteItem.SetSelectedAzeriteEssence(azeriteEssenceActivateEssence.Slot, azeriteEssenceActivateEssence.AzeriteEssenceID);

        _session.Player.ApplyAzeriteEssence(azeriteItem,
                                    azeriteEssenceActivateEssence.AzeriteEssenceID,
                                    rank,
                                    (AzeriteItemMilestoneType)_db2Manager.GetAzeriteItemMilestonePower(azeriteEssenceActivateEssence.Slot).Type == AzeriteItemMilestoneType.MajorEssence,
                                    true);

        azeriteItem.SetState(ItemUpdateState.Changed, _session.Player);
    }

    [WorldPacketHandler(ClientOpcodes.AzeriteEssenceUnlockMilestone, Processing = PacketProcessing.Inplace)]
    private void HandleAzeriteEssenceUnlockMilestone(AzeriteEssenceUnlockMilestone azeriteEssenceUnlockMilestone)
    {
        if (_azeriteItem.FindHeartForge(_session.Player) == null)
            return;

        var item = _session.Player.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);

        var azeriteItem = item?.AsAzeriteItem;

        if (azeriteItem == null || !azeriteItem.CanUseEssences())
            return;

        var milestonePower = _azeriteItemMilestonePowerRecords.LookupByKey(azeriteEssenceUnlockMilestone.AzeriteItemMilestonePowerID);

        if (milestonePower == null || milestonePower.RequiredLevel > azeriteItem.GetLevel())
            return;

        // check that all previous milestones are unlocked
        foreach (var previousMilestone in _db2Manager.GetAzeriteItemMilestonePowers())
        {
            if (previousMilestone == milestonePower)
                break;

            if (!azeriteItem.HasUnlockedEssenceMilestone(previousMilestone.Id))
                return;
        }

        azeriteItem.AddUnlockedEssenceMilestone(milestonePower.Id);
        _session.Player.ApplyAzeriteItemMilestonePower(azeriteItem, milestonePower, true);
        azeriteItem.SetState(ItemUpdateState.Changed, _session.Player);
    }
}