// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock;

// 234877 - Curse of Shadows
[SpellScript(234877)]
public class spell_warlock_curse_of_shadows : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        var caster = Caster;

        if (caster == null)
            return;

        var spellInfo = eventInfo.DamageInfo.SpellInfo;

        if (spellInfo == null || (spellInfo.GetSchoolMask() & SpellSchoolMask.Shadow) == 0)
            return;

        var damage = MathFunctions.CalculatePct(eventInfo.DamageInfo.Damage, aurEff.Amount);
        caster.CastSpell(eventInfo.ActionTarget, WarlockSpells.CURSE_OF_SHADOWS_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)damage));
    }
}