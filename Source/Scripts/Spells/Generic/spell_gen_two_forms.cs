// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenTwoForms : SpellScript, ISpellCheckCast, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public SpellCastResult CheckCast()
    {
        if (Caster.IsInCombat)
        {
            SetCustomCastResultMessage(SpellCustomErrors.CantTransform);

            return SpellCastResult.CustomError;
        }

        // Player cannot transform to human form if he is forced to be worgen for some reason (Darkflight)
        if (Caster.GetAuraEffectsByType(AuraType.WorgenAlteredForm).Count > 1)
        {
            SetCustomCastResultMessage(SpellCustomErrors.CantTransform);

            return SpellCastResult.CustomError;
        }

        return SpellCastResult.SpellCastOk;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleTransform, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleTransform(int effIndex)
    {
        var target = HitUnit;
        PreventHitDefaultEffect(effIndex);

        if (target.HasAuraType(AuraType.WorgenAlteredForm))
            target.RemoveAurasByType(AuraType.WorgenAlteredForm);
        else // Basepoints 1 for this aura control whether to trigger transform transition animation or not.
            target.SpellFactory.CastSpell(target, GenericSpellIds.ALTERED_FORM, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, 1));
    }
}