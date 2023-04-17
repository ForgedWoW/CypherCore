// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 67228 - Item - Shaman T9 Elemental 4P Bonus (Lava Burst)
[SpellScript(67228)]
internal class SpellShaT9Elemental4PBonus : AuraScript, IHasAuraEffects
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

        var spellInfo = Global.SpellMgr.GetSpellInfo(ShamanSpells.LAVA_BURST_BONUS_DAMAGE, CastDifficulty);
        var amount = (int)MathFunctions.CalculatePct(damageInfo.Damage, aurEff.Amount);
        amount /= (int)spellInfo.MaxTicks;

        var caster = eventInfo.Actor;
        var target = eventInfo.ProcTarget;

        CastSpellExtraArgs args = new(aurEff);
        args.AddSpellMod(SpellValueMod.BasePoint0, amount);
        caster.SpellFactory.CastSpell(target, ShamanSpells.LAVA_BURST_BONUS_DAMAGE, args);
    }
}