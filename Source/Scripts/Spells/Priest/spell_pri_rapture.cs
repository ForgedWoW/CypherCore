// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[Script] // 47536 - Rapture
internal class SpellPriRapture : SpellScript, ISpellAfterCast, IHasSpellEffects
{
    private ObjectGuid _raptureTarget;

    public List<ISpellEffect> SpellEffects { get; } = new();


    public void AfterCast()
    {
        var caster = Caster;
        var target = Global.ObjAccessor.GetUnit(caster, _raptureTarget);

        if (target != null)
            caster.SpellFactory.CastSpell(target, PriestSpells.POWER_WORD_SHIELD, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnorePowerAndReagentCost | TriggerCastFlags.IgnoreCastInProgress).SetTriggeringSpell(Spell));
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleEffectDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleEffectDummy(int effIndex)
    {
        _raptureTarget = HitUnit.GUID;
    }
}