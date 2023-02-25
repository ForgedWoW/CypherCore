// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	[SpellScript(WarlockSpells.BLASPHEMY)]
	public class aura_warl_blasphemy : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

		private void Apply(AuraEffect aura, AuraEffectHandleModes auraMode)
		{
			if (GetCaster().TryGetAsPlayer(out var p) && p.TryGetAura(WarlockSpells.AVATAR_OF_DESTRUCTION, out var avatar))
			{
				var time = avatar.GetEffect(0).Amount * Time.InMilliseconds;
				SetMaxDuration(time);
				SetDuration(time);
			}
        }

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(Apply, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
		}
    }
}