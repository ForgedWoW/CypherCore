// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[SpellScript(190984)]
public class SpellDruidSolarWrath : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleHitTarget(int effIndex)
    {
        var target = HitUnit;

        if (target != null)
            if (Caster.HasAura(Spells.NaturesBalance))
            {
                var sunfireDot = target.GetAura(Spells.SunfireDot, Caster.GUID);

                if (sunfireDot != null)
                {
                    var duration = sunfireDot.Duration;
                    var newDuration = duration + 4 * Time.IN_MILLISECONDS;

                    if (newDuration > sunfireDot.MaxDuration)
                        sunfireDot.SetMaxDuration(newDuration);

                    sunfireDot.SetDuration(newDuration);
                }
            }

        if (Caster && RandomHelper.randChance(20) && Caster.HasAura(DruidSpells.Eclipse))
            Caster.SpellFactory.CastSpell(null, DruidSpells.LunarEmpowerement, true);
    }


    private struct Spells
    {
        public static readonly uint SolarWrath = 190984;
        public static readonly uint NaturesBalance = 202430;
        public static readonly uint SunfireDot = 164815;
    }
}