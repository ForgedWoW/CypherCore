// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[Script] // 70723 - Item - Druid T10 Balance 4P Bonus
internal class SpellDruT10Balance4PBonus : AuraScript, IHasAuraEffects
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

        var caster = eventInfo.Actor;
        var target = eventInfo.ProcTarget;

        var spellInfo = Global.SpellMgr.GetSpellInfo(DruidSpellIds.Languish, CastDifficulty);
        var amount = (int)MathFunctions.CalculatePct(damageInfo.Damage, aurEff.Amount);
        amount /= (int)spellInfo.MaxTicks;

        CastSpellExtraArgs args = new(aurEff);
        args.AddSpellMod(SpellValueMod.BasePoint0, amount);
        caster.SpellFactory.CastSpell(target, DruidSpellIds.Languish, args);
    }
}