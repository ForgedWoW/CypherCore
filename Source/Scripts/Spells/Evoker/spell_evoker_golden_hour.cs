// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.BRONZE_REVERSION)]
public class SpellEvokerGoldenHour : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        if (Caster.TryGetAura(EvokerSpells.BRONZE_GOLDEN_HOUR, out var gaAura) && ExplTargetUnit.TryGetAsPlayer(out var target))
        {
            var args = new CastSpellExtraArgs(TriggerCastFlags.TriggeredAllowProc);
            args.AddSpellMod(SpellValueMod.BasePoint0, target.GetDamageOverLastSeconds((uint)gaAura.SpellInfo.GetEffect(1).BasePoints) * (gaAura.SpellInfo.GetEffect(0).BasePoints * 0.01));
            Caster.SpellFactory.CastSpell(target, EvokerSpells.BRONZE_GOLDEN_HOUR_HEAL, args);
        }
    }
}