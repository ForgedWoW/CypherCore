// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 64928 - Item - Shaman T8 Elemental 4P Bonus
[SpellScript(64928)]
internal class spell_sha_t8_elemental_4p_bonus : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        var damageInfo = eventInfo.DamageInfo;

        if (damageInfo == null ||
            damageInfo.Damage == 0)
            return;

        var spellInfo = Global.SpellMgr.GetSpellInfo(ShamanSpells.Electrified, CastDifficulty);
        var amount = (int)MathFunctions.CalculatePct(damageInfo.Damage, aurEff.Amount);
        amount /= (int)spellInfo.MaxTicks;

        var caster = eventInfo.Actor;
        var target = eventInfo.ProcTarget;

        CastSpellExtraArgs args = new(aurEff);
        args.AddSpellMod(SpellValueMod.BasePoint0, amount);
        caster.CastSpell(target, ShamanSpells.Electrified, args);
    }
}