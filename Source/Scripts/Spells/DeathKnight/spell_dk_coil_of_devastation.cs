// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[SpellScript(390270)]
public class SpellDkCoilOfDevastation : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public bool CheckProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.DamageInfo != null)
            return eventInfo.DamageInfo.SpellInfo.Id == DeathKnightSpells.DEATH_COIL_DAMAGE;

        return false;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        // TODO: This doesn't seem to actually do damage
        PreventDefaultAction();
        var devDot = Global.SpellMgr.GetSpellInfo(DeathKnightSpells.DEATH_COIL_DEVASTATION_DOT);
        var pct = aurEff.Amount;
        var amount = (int)(MathFunctions.CalculatePct(eventInfo.DamageInfo.Damage, pct) / devDot.MaxTicks);

        var args = new CastSpellExtraArgs(aurEff);
        args.SpellValueOverrides[SpellValueMod.BasePoint0] = amount;
        Target.SpellFactory.CastSpell(eventInfo.ProcTarget, DeathKnightSpells.DEATH_COIL_DEVASTATION_DOT, args);
    }
}