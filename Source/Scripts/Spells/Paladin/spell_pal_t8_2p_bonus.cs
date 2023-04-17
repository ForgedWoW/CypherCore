// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

[SpellScript(64890)] // 64890 - Item - Paladin T8 Holy 2P Bonus
internal class SpellPalT82PBonus : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        var healInfo = eventInfo.HealInfo;

        if (healInfo == null ||
            healInfo.Heal == 0)
            return;

        var caster = eventInfo.Actor;
        var target = eventInfo.ProcTarget;

        var spellInfo = Global.SpellMgr.GetSpellInfo(PaladinSpells.HOLY_MENDING, CastDifficulty);
        var amount = (int)MathFunctions.CalculatePct(healInfo.Heal, aurEff.Amount);
        amount /= (int)spellInfo.MaxTicks;

        CastSpellExtraArgs args = new(aurEff);
        args.AddSpellMod(SpellValueMod.BasePoint0, amount);
        caster.SpellFactory.CastSpell(target, PaladinSpells.HOLY_MENDING, args);
    }
}