// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Crafting;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

internal class ItemPushResult : ServerPacket
{
    public enum DisplayType
    {
        Hidden = 0,
        Normal = 1,
        EncounterLoot = 2
    }

    public int BattlePetBreedID;

    public uint BattlePetBreedQuality;

    public int BattlePetLevel;

    public int BattlePetSpeciesID;

    public CraftingData CraftingData;

    public bool Created;

    public DisplayType DisplayText;

    public int DungeonEncounterID;

    public uint? FirstCraftOperationID;

    public bool IsBonusRoll;

    public bool IsEncounterLoot;

    public ItemInstance Item;

    public ObjectGuid ItemGUID;

    public ObjectGuid PlayerGUID;

    public bool Pushed;

    // only set if different than real ID (similar to CreatureTemplate.KillCredit)
    public uint Quantity;

    public uint QuantityInInventory;

    public int QuestLogItemID;

    public byte Slot;

    public int SlotInBag;

    // Item ID used for updating quest progress
    public List<UiEventToast> Toasts = new();

    public ItemPushResult() : base(ServerOpcodes.ItemPushResult) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(PlayerGUID);
        WorldPacket.WriteUInt8(Slot);
        WorldPacket.WriteInt32(SlotInBag);
        WorldPacket.WriteInt32(QuestLogItemID);
        WorldPacket.WriteUInt32(Quantity);
        WorldPacket.WriteUInt32(QuantityInInventory);
        WorldPacket.WriteInt32(DungeonEncounterID);
        WorldPacket.WriteInt32(BattlePetSpeciesID);
        WorldPacket.WriteInt32(BattlePetBreedID);
        WorldPacket.WriteUInt32(BattlePetBreedQuality);
        WorldPacket.WriteInt32(BattlePetLevel);
        WorldPacket.WritePackedGuid(ItemGUID);
        WorldPacket.WriteInt32(Toasts.Count);

        foreach (var uiEventToast in Toasts)
            uiEventToast.Write(WorldPacket);

        WorldPacket.WriteBit(Pushed);
        WorldPacket.WriteBit(Created);
        WorldPacket.WriteBits((uint)DisplayText, 3);
        WorldPacket.WriteBit(IsBonusRoll);
        WorldPacket.WriteBit(IsEncounterLoot);
        WorldPacket.WriteBit(CraftingData != null);
        WorldPacket.WriteBit(FirstCraftOperationID.HasValue);
        WorldPacket.FlushBits();

        Item.Write(WorldPacket);

        if (FirstCraftOperationID.HasValue)
            WorldPacket.WriteUInt32(FirstCraftOperationID.Value);

        CraftingData?.Write(WorldPacket);
    }
}