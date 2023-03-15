// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman;

// 318038 - Flametongue Weapon
[SpellScript(318038)]
internal class spell_sha_flametongue_weapon : SpellScript, ISpellOnCast, ISpellCheckCast
{
	Item _item;

	public SpellCastResult CheckCast()
	{
		var player = Caster.AsPlayer;
		var slot = EquipmentSlot.MainHand;

		if (player.GetPrimarySpecialization() == TalentSpecialization.ShamanEnhancement)
			slot = EquipmentSlot.OffHand;

		_item = player.GetItemByPos(InventorySlots.Bag0, slot);

		return _item == null || !_item.Template.IsWeapon ? SpellCastResult.TargetNoWeapons : SpellCastResult.SpellCastOk;
	}


	public override bool Load()
	{
		return Caster.IsTypeId(TypeId.Player);
	}

	public void OnCast()
	{
		Caster.CastSpell(_item, ShamanSpells.FlametongueWeaponEnchant, true);
	}
}