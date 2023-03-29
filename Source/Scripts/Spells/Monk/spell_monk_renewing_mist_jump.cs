// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(119607)]
public class spell_monk_renewing_mist_jump : SpellScript, IHasSpellEffects
{
    private ObjectGuid _previousTargetGuid = new();
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(HandleTargets, 1, Targets.UnitDestAreaAlly));
        SpellEffects.Add(new EffectHandler(HandleHit, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleTargets(List<WorldObject> targets)
    {
        var caster = Caster;
        var previousTarget = ExplTargetUnit;

        // Not remove full health targets now, dancing mists talent can jump on full health too


        targets.RemoveIf((WorldObject a) =>
        {
            var ally = a.AsUnit;

            if (ally == null || ally.HasAura(MonkSpells.RENEWING_MIST_HOT, caster.GUID) || ally == previousTarget)
                return true;

            return false;
        });

        targets.RemoveIf((WorldObject a) =>
        {
            var ally = a.AsUnit;

            if (ally == null || ally.IsFullHealth)
                return true;

            return false;
        });

        if (targets.Count > 1)
        {
            targets.Sort(new HealthPctOrderPred());
            targets.Resize(1);
        }

        _previousTargetGuid = previousTarget.GUID;
    }

    private void HandleHit(int effIndex)
    {
        PreventHitDefaultEffect(effIndex);
        var caster = Caster;
        var previousTarget = ObjectAccessor.Instance.GetUnit(caster, _previousTargetGuid);

        if (previousTarget != null)
        {
            var oldAura = previousTarget.GetAura(MonkSpells.RENEWING_MIST_HOT, Caster.GUID);

            if (oldAura != null)
            {
                var newAura = caster.AddAura(MonkSpells.RENEWING_MIST_HOT, HitUnit);

                if (newAura != null)
                {
                    newAura.SetDuration(oldAura.Duration);
                    previousTarget.SendPlaySpellVisual(HitUnit.Location, previousTarget.Location.Orientation, MonkSpells.VISUAL_RENEWING_MIST, 0, 0, 50.0f, false);
                    oldAura.Remove();
                }
            }
        }
    }
}