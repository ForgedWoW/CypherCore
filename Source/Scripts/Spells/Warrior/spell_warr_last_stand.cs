// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

/// Updated 8.3.7
// 12975 - Last Stand
[SpellScript(12975)]
public class SpellWarrLastStand : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster;
        var args = new CastSpellExtraArgs(TriggerCastFlags.FullMask);
        args.AddSpellMod(SpellValueMod.BasePoint0, (int)caster.CountPctFromMaxHealth(EffectValue));
        caster.SpellFactory.CastSpell(caster, WarriorSpells.LAST_STAND_TRIGGERED, args);
    }
}