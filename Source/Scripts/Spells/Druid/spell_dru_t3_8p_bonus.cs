// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[Script] // 28719 - Healing Touch
internal class SpellDruT38PBonus : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        var spell = eventInfo.ProcSpell;

        if (spell == null)
            return;

        var caster = eventInfo.Actor;
        var spellPowerCostList = spell.PowerCost;
        var spellPowerCost = spellPowerCostList.First(cost => cost.Power == PowerType.Mana);

        if (spellPowerCost == null)
            return;

        var amount = MathFunctions.CalculatePct(spellPowerCost.Amount, aurEff.Amount);
        CastSpellExtraArgs args = new(aurEff);
        args.AddSpellMod(SpellValueMod.BasePoint0, amount);
        caster.SpellFactory.CastSpell((Unit)null, DruidSpellIds.Exhilarate, args);
    }
}