// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

//187880 - Maelstrom Weapon
[SpellScript(187880)]
public class spell_sha_maelstrom_weapon : AuraScript, IHasAuraEffects, IAuraCheckProc
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public bool CheckProc(ProcEventInfo info)
	{
		return info.DamageInfo.AttackType == WeaponAttackType.BaseAttack || info.DamageInfo.AttackType == WeaponAttackType.OffAttack || info.SpellInfo.Id == ShamanSpells.WINDFURY_ATTACK;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	public void HandleEffectProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
	{
		var caster = Caster;

		if (caster != null)
			caster.CastSpell(caster, ShamanSpells.MAELSTROM_WEAPON_POWER, true);
	}
}