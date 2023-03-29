// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman;

// 77478 - Earthquake tick
[SpellScript(77478)]
internal class spell_sha_earthquake_tick : SpellScript, ISpellOnHit, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public void OnHit()
    {
        var target = HitUnit;

        if (target != null)
            if (RandomHelper.randChance(GetEffectInfo(1).CalcValue()))
            {
                var areaTriggers = Caster.GetAreaTriggers(ShamanSpells.Earthquake);
                var foundAreaTrigger = areaTriggers.Find(at => at.GUID == Spell.OriginalCasterGuid);

                if (foundAreaTrigger != null)
                    foundAreaTrigger.ForEachAreaTriggerScript<IAreaTriggerScriptValues>(a =>
                    {
                        if (a.ScriptValues.TryAdd(target.GUID.ToString(), target.GUID))
                            Caster.CastSpell(target, ShamanSpells.EarthquakeKnockingDown, true);
                    });
            }
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDamageCalc, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.LaunchTarget));
    }

    private void HandleDamageCalc(int effIndex)
    {
        EffectValue = (int)(Caster.SpellBaseDamageBonusDone(SpellSchoolMask.Nature) * 0.391f);
    }
}