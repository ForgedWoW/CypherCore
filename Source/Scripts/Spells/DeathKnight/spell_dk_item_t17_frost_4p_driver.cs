// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[SpellScript(167655)]
public class SpellDkItemT17Frost4PDriver : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
    }

    private void OnProc(AuraEffect unnamedParameter, ProcEventInfo pEventInfo)
    {
        PreventDefaultAction();

        var lCaster = Caster;

        if (lCaster == null)
            return;

        var lProcSpell = pEventInfo.DamageInfo.SpellInfo;

        if (lProcSpell == null)
            return;

        var lTarget = pEventInfo.ActionTarget;

        if (lTarget == null || lTarget == lCaster)
            return;

        /// While Pillar of Frost is active, your special attacks trap a soul in your rune weapon.
        lCaster.SpellFactory.CastSpell(lTarget, ESpells.FROZEN_RUNEBLADE, true);
    }

    private struct ESpells
    {
        public const uint FROZEN_RUNEBLADE = 170202;
    }
}