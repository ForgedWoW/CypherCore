// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

[SpellScript(37377, "spell_warl_t4_2p_bonus_shadow", false, WarlockSpells.FLAMESHADOW)] // 37377 - Shadowflame
[SpellScript(39437, "spell_warl_t4_2p_bonus_fire", false, WarlockSpells.SHADOWFLAME)]   // 39437 - Shadowflame Hellfire and RoF
internal class SpellWarlT42PBonus : AuraScript, IHasAuraEffects
{
    private readonly uint _triggerSpell;

    public SpellWarlT42PBonus(uint triggerSpell)
    {
        _triggerSpell = triggerSpell;
    }

    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        var caster = eventInfo.Actor;
        caster.SpellFactory.CastSpell(caster, _triggerSpell, new CastSpellExtraArgs(aurEff));
    }
}