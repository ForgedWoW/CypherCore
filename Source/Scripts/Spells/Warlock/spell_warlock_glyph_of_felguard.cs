// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

// 30146, 112870 - Summon Felguard, Summon Wrathguard
[SpellScript(new uint[]
{
	30146, 112870
})]
public class spell_warlock_glyph_of_felguard : SpellScript, ISpellAfterHit
{
	public void AfterHit()
	{
		var caster = Caster.AsPlayer;

		if (caster != null)
		{
			if (!caster.HasAura(WarlockSpells.GLYPH_OF_FELGUARD))
				return;

			uint itemEntry = 0;

			for (int i = InventorySlots.ItemStart; i < InventorySlots.ItemEnd; ++i)
			{
				var pItem = caster.GetItemByPos(InventorySlots.Bag0, (byte)i);

				if (pItem != null)
				{
					var itemplate = pItem.Template;

					if (itemplate != null)
						if (itemplate.Class == ItemClass.Weapon && (itemplate.SubClass == (uint)ItemSubClassWeapon.Sword2 || itemplate.SubClass == (uint)ItemSubClassWeapon.Axe2 || itemplate.SubClass == (uint)ItemSubClassWeapon.Exotic2 || itemplate.SubClass == (uint)ItemSubClassWeapon.Mace2 || itemplate.SubClass == (uint)ItemSubClassWeapon.Polearm))
						{
							itemEntry = itemplate.Id;

							break;
						}
				}
			}


			var pet = ObjectAccessor.GetPet(caster, caster.PetGUID);

			if (pet != null)
			{
				for (byte i = 0; i < 3; ++i)
					pet.SetVirtualItem(i, 0);

				pet.SetVirtualItem(0, itemEntry);
			}
		}
	}
}