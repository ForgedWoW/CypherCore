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

namespace Scripts.Spells.Warlock;

// Soul Conduit - 215941
[SpellScript(215941)]
public class SpellWarlSoulConduit : AuraScript, IHasAuraEffects, IAuraCheckProc
{
    private int _refund = 0;

    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var caster = Caster;

        if (caster == null)
            return false;

        if (eventInfo.Actor && eventInfo.Actor != caster)
            return false;

        var spell = eventInfo.ProcSpell;

        if (spell == null)
        {
            var costs = spell.PowerCost;

            var costData = costs.FirstOrDefault(cost => cost.Power == PowerType.Mana && cost.Amount > 0);

            if (costData == null)
                return false;

            _refund = costData.Amount;

            return true;
        }

        return false;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect unnamedParameter, ProcEventInfo unnamedParameter2)
    {
        var caster = Caster;

        if (caster == null)
            return;

        if (RandomHelper.randChance(SpellInfo.GetEffect(0).BasePoints))
            caster.SpellFactory.CastSpell(caster, WarlockSpells.SOUL_CONDUIT_REFUND, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)_refund));
    }
}