// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Paladin
{
    [SpellScript(234299)] // 234299 - Fist of Justice
    internal class spell_pal_fist_of_justice : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(PaladinSpells.HammerOfJustice);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraCheckEffectProcHandler(CheckEffectProc, 0, AuraType.Dummy));
            AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private bool CheckEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Spell procSpell = eventInfo.GetProcSpell();

            if (procSpell != null)
                return procSpell.HasPowerTypeCost(PowerType.HolyPower);

            return false;
        }

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            var value = aurEff.Amount / 10;

            GetTarget().GetSpellHistory().ModifyCooldown(PaladinSpells.HammerOfJustice, TimeSpan.FromSeconds(-value));
        }
    }
}
