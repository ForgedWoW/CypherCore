// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// 196277 - Implosion
[SpellScript(WarlockSpells.IMPLOSION)]
public class SpellWarlImplosion : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public void HandleHit(int effIndex)
    {
        var caster = Caster;
        var target = HitUnit;

        if (caster == null ||
            target == null)
            return;

        var imps = caster.GetCreatureListWithEntryInGrid(55659); // Wild Imps

        foreach (var imp in imps)
            if (imp.ToTempSummon().GetSummoner() == caster)
            {
                imp.InterruptNonMeleeSpells(false);
                imp.VariableStorage.Set("controlled", true);
                imp.VariableStorage.Set("ForceUpdateTimers", true);
                imp.SpellFactory.CastSpell(target, WarlockSpells.IMPLOSION_JUMP, true);
                imp.MotionMaster.MoveJump(target.Location, 300.0f, 1.0f, EventId.Jump);
                imp.SendUpdateToPlayer(caster.AsPlayer);
                var casterGuid = caster.GUID;

                imp.Events.AddEventAtOffset(() =>
                                            {
                                                imp.SpellFactory.CastSpell(imp, WarlockSpells.IMPLOSION_DAMAGE, new CastSpellExtraArgs(SpellValueMod.BasePoint0, (int)GetEffectInfo(1).Amplitude).SetOriginalCaster(casterGuid).SetTriggerFlags(TriggerCastFlags.FullMask));
                                                imp.DisappearAndDie();
                                            },
                                            TimeSpan.FromMilliseconds(500));
            }

        caster.RemoveAura(296553);
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }
}