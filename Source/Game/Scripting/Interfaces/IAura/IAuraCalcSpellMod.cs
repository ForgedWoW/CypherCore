﻿using Framework.Constants;
using Game.Spells;

namespace Game.Scripting.Interfaces.IAura
{
	public interface IAuraCalcSpellMod : IAuraEffectHandler
	{
		void CalcSpellMod(AuraEffect aura, ref SpellModifier spellMod);
	}

	public class EffectCalcSpellModHandler : AuraEffectHandler, IAuraCalcSpellMod
	{
		public delegate void AuraEffectCalcSpellModDelegate(AuraEffect aura, ref SpellModifier spellMod);

		private AuraEffectCalcSpellModDelegate _fn;

		public EffectCalcSpellModHandler(AuraEffectCalcSpellModDelegate fn, uint effectIndex, AuraType auraType) : base(effectIndex, auraType, AuraScriptHookType.EffectCalcSpellmod)
		{
			_fn = fn;
		}

		public void CalcSpellMod(AuraEffect aura, ref SpellModifier spellMod)
		{
			_fn(aura, ref spellMod);
		}
	}
}