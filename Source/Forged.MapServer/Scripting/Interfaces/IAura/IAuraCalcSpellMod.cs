// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Forged.MapServer.Scripting.Interfaces.IAura;

public interface IAuraCalcSpellMod : IAuraEffectHandler
{
	void CalcSpellMod(AuraEffect aura, SpellModifier spellMod);
}

public class AuraEffectCalcSpellModHandler : AuraEffectHandler, IAuraCalcSpellMod
{
	private readonly Action<AuraEffect, SpellModifier> _fn;

	public AuraEffectCalcSpellModHandler(Action<AuraEffect, SpellModifier> fn, int effectIndex, AuraType auraType) : base(effectIndex, auraType, AuraScriptHookType.EffectCalcSpellmod)
	{
		_fn = fn;
	}

	public void CalcSpellMod(AuraEffect aura, SpellModifier spellMod)
	{
		_fn(aura, spellMod);
	}
}