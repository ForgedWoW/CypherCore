// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Paladin
{
    [SpellScript(267610)] // 267610 - Righteous Verdict
    internal class spell_pal_righteous_verdict : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(PaladinSpells.RighteousVerdictAura);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            procInfo.GetActor().CastSpell(procInfo.GetActor(), PaladinSpells.RighteousVerdictAura, true);
        }
    }
}
