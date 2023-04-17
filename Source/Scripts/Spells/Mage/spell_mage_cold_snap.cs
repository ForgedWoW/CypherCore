// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[Script] // 235219 - Cold Snap
internal class SpellMageColdSnap : SpellScript, IHasSpellEffects
{
    private static readonly uint[] SpellsToReset =
    {
        MageSpells.ConeOfCold, MageSpells.IceBarrier, MageSpells.IceBlock
    };

    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHit));
    }

    private void HandleDummy(int effIndex)
    {
        foreach (var spellId in SpellsToReset)
            Caster.SpellHistory.ResetCooldown(spellId, true);

        Caster.SpellHistory.RestoreCharge(Global.SpellMgr.GetSpellInfo(MageSpells.FrostNova, CastDifficulty).ChargeCategoryId);
    }
}