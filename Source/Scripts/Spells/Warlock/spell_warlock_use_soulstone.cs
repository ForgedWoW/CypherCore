// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// 3026 - Use Soulstone
[SpellScript(3026)]
public class SpellWarlockUseSoulstone : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SelfResurrect, SpellScriptHookType.EffectHit));
    }

    private void HandleHit(int effIndex)
    {
        PreventHitDefaultEffect(effIndex);
        var player = Caster.AsPlayer;

        if (player == null)
            return;

        var originalCaster = OriginalCaster;

        // already have one active request
        if (player.IsResurrectRequested)
            return;

        var healthPct = SpellInfo.GetEffect(1).CalcValue(originalCaster);
        var manaPct = SpellInfo.GetEffect(0).CalcValue(originalCaster);

        var health = player.CountPctFromMaxHealth(healthPct);
        var mana = 0;

        if (player.GetMaxPower(PowerType.Mana) > 0)
            mana = MathFunctions.CalculatePct(player.GetMaxPower(PowerType.Mana), manaPct);

        player.ResurrectPlayer(0.0f);
        player.SetHealth(health);
        player.SetPower(PowerType.Mana, mana);
        player.SetPower(PowerType.Rage, 0);
        player.SetPower(PowerType.Energy, player.GetMaxPower(PowerType.Energy));
        player.SetPower(PowerType.Focus, 0);
        player.SpawnCorpseBones();
    }
}