// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 77762 - Lava Surge
[SpellScript(77762)]
internal class spell_sha_lava_surge_proc : SpellScript, ISpellAfterHit
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.LavaBurst);
	}

	public override bool Load()
	{
		return Caster.IsTypeId(TypeId.Player);
	}

	public void AfterHit()
	{
		Caster.GetSpellHistory().RestoreCharge(Global.SpellMgr.GetSpellInfo(ShamanSpells.LavaBurst, CastDifficulty).ChargeCategoryId);
	}
}