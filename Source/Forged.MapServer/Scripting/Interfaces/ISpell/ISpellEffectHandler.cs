// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Scripting.Interfaces.ISpell;

public interface ISpellEffectHandler : ISpellEffect
{
	SpellEffectName EffectName { get; }

	void CallEffect(int effIndex);
}

public class EffectHandler : SpellEffect, ISpellEffectHandler
{
	private readonly Action<int> _callEffect;

	public SpellEffectName EffectName { get; private set; }

	public EffectHandler(Action<int> callEffect, int effectIndex, SpellEffectName spellEffectName, SpellScriptHookType hookType) : base(effectIndex, hookType)
	{
		EffectName = spellEffectName;
		_callEffect = callEffect;
	}

	public void CallEffect(int effIndex)
	{
		_callEffect(EffectIndex);
	}
}