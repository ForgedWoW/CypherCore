// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[SpellScript(44614)]
public class SpellMageFlurry : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster;
        var target = HitUnit;
        var isImproved = false;

        if (caster == null || target == null)
            return;

        if (caster.HasAura(MageSpells.BRAIN_FREEZE_AURA))
        {
            caster.RemoveAura(MageSpells.BRAIN_FREEZE_AURA);

            if (caster.HasSpell(MageSpells.BRAIN_FREEZE_IMPROVED))
                isImproved = true;
        }

        var targetGuid = target.GUID;

        if (targetGuid != ObjectGuid.Empty)
            for (byte i = 1; i < 3; ++i) // basepoint value is 3 all the time, so, set it 3 because sometimes it won't read
                caster.Events.AddEventAtOffset(() =>
                                               {
                                                   if (caster != null)
                                                   {
                                                       var target = ObjectAccessor.Instance.GetUnit(caster, targetGuid);

                                                       if (target != null)
                                                       {
                                                           caster.SpellFactory.CastSpell(target, MageSpells.FLURRY_VISUAL, false);

                                                           if (isImproved)
                                                               caster.SpellFactory.CastSpell(target, MageSpells.FLURRY_CHILL_PROC, false);
                                                       }
                                                   }
                                               },
                                               TimeSpan.FromMilliseconds(i * 250));
    }
}