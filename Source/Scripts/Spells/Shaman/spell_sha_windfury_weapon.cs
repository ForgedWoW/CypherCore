// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman;

// 33757 - Windfury Weapon
[SpellScript(33757)]
internal class spell_sha_windfury_weapon : SpellScript, ISpellOnCast, ISpellCheckCast
{
	Item _item;

	public SpellCastResult CheckCast()
	{
		_item = Caster.AsPlayer.GetWeaponForAttack(WeaponAttackType.BaseAttack, false);

		return _item == null || !_item.GetTemplate().IsWeapon() ? SpellCastResult.TargetNoWeapons : SpellCastResult.SpellCastOk;
	}


	public override bool Load()
	{
		return Caster.IsPlayer;
	}

	public void OnCast()
	{
		Caster.CastSpell(_item, ShamanSpells.WindfuryEnchantment, Spell);
	}
}