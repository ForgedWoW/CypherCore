// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Rogue;

[Script] // 185438 - Shadowstrike
internal class SpellRogShadowstrike : SpellScript, ISpellCheckCast, IHasSpellEffects
{
    private bool _hasPremeditationAura = false;

    public List<ISpellEffect> SpellEffects { get; } = new();


    public SpellCastResult CheckCast()
    {
        // Because the premeditation aura is removed when we're out of stealth,
        // when we reach HandleEnergize the aura won't be there, even if it was when player launched the spell
        _hasPremeditationAura = Caster.HasAura(RogueSpells.PremeditationAura);

        return SpellCastResult.Success;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleEnergize, 1, SpellEffectName.Energize, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleEnergize(int effIndex)
    {
        var caster = Caster;

        if (_hasPremeditationAura)
        {
            if (caster.HasAura(RogueSpells.SliceAndDice))
            {
                var premeditationPassive = caster.GetAura(RogueSpells.PremeditationPassive);

                if (premeditationPassive != null)
                {
                    var auraEff = premeditationPassive.GetEffect(1);

                    if (auraEff != null)
                        HitDamage = HitDamage + auraEff.Amount;
                }
            }

            // Grant 10 seconds of slice and dice
            var duration = Global.SpellMgr.GetSpellInfo(RogueSpells.PremeditationPassive, Difficulty.None).GetEffect(0).CalcValue(Caster);

            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.Duration, duration * Time.IN_MILLISECONDS);
            caster.SpellFactory.CastSpell(caster, RogueSpells.SliceAndDice, args);
        }
    }
}