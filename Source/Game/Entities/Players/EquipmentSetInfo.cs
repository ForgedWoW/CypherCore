// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Entities;

public class EquipmentSetInfo
{
	public enum EquipmentSetType
	{
		Equipment = 0,
		Transmog = 1
	}

	public EquipmentSetUpdateState State { get; set; }
	public EquipmentSetData Data { get; set; }

	public EquipmentSetInfo()
	{
		State = EquipmentSetUpdateState.New;
		Data = new EquipmentSetData();
	}

	// Data sent in EquipmentSet related packets
	public class EquipmentSetData
	{
		public EquipmentSetType Type { get; set; }
		public ulong Guid { get; set; }                  // Set Identifier
		public uint SetId { get; set; }                  // Index
		public uint IgnoreMask { get; set; }             // Mask of EquipmentSlot
		public int AssignedSpecIndex { get; set; } = -1; // Index of character specialization that this set is automatically equipped for
		public string SetName { get; set; } = "";
		public string SetIcon { get; set; } = "";
		public ObjectGuid[] Pieces { get; set; } = new ObjectGuid[EquipmentSlot.End];
		public int[] Appearances { get; set; } = new int[EquipmentSlot.End]; // ItemModifiedAppearanceID
		public int[] Enchants { get; set; } = new int[2];                    // SpellItemEnchantmentID
		public int SecondaryShoulderApparanceId { get; set; }                // Secondary shoulder appearance
		public int SecondaryShoulderSlot { get; set; }                       // Always 2 if secondary shoulder apperance is used
		public int SecondaryWeaponAppearanceId { get; set; }                 // For legion artifacts: linked child item appearance
		public int SecondaryWeaponSlot { get; set; }                         // For legion artifacts: which slot is used by child item
	}
}