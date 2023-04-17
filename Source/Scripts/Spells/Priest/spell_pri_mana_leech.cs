// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[SpellScript(28305)]
public class SpellPriManaLeech : AuraScript, IHasAuraEffects, IAuraCheckProc
{
    private Unit _procTarget;

    public SpellPriManaLeech()
    {
        _procTarget = null;
    }

    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public bool CheckProc(ProcEventInfo unnamedParameter)
    {
        _procTarget = Target.OwnerUnit;

        return _procTarget != null;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo unnamedParameter)
    {
        PreventDefaultAction();
        Target.SpellFactory.CastSpell(_procTarget, PriestSpells.MANA_LEECH_PROC, aurEff);
    }
}