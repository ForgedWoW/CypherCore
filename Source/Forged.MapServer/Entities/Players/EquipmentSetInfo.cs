// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Entities.Players;

public class EquipmentSetInfo
{
    public EquipmentSetInfo()
    {
        State = EquipmentSetUpdateState.New;
        Data = new EquipmentSetData();
    }

    public enum EquipmentSetType
    {
        Equipment = 0,
        Transmog = 1
    }

    public EquipmentSetData Data { get; set; }
    public EquipmentSetUpdateState State { get; set; }
    // Data sent in EquipmentSet related packets
    public class EquipmentSetData
    {
        public int[] Appearances { get; set; } = new int[EquipmentSlot.End];
        public int AssignedSpecIndex { get; set; } = -1;
        // ItemModifiedAppearanceID
        public int[] Enchants { get; set; } = new int[2];

        public ulong Guid { get; set; }
        public uint IgnoreMask { get; set; }
        public ObjectGuid[] Pieces { get; set; } = new ObjectGuid[EquipmentSlot.End];
        // SpellItemEnchantmentID
        public int SecondaryShoulderApparanceId { get; set; }

        // Secondary shoulder appearance
        public int SecondaryShoulderSlot { get; set; }

        // Always 2 if secondary shoulder apperance is used
        public int SecondaryWeaponAppearanceId { get; set; }

        // For legion artifacts: linked child item appearance
        public int SecondaryWeaponSlot { get; set; }

        public string SetIcon { get; set; } = "";
        // Set Identifier
        public uint SetId { get; set; }

        // Index
        // Mask of EquipmentSlot
        // Index of character specialization that this set is automatically equipped for
        public string SetName { get; set; } = "";

        public EquipmentSetType Type { get; set; }
        // For legion artifacts: which slot is used by child item
    }
}