// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

[SpellScript(218679)]
public class SpellDhSpiritBomb : SpellScript, ISpellOnHit, ISpellCheckCast
{
    readonly uint[] _ids = new uint[]
    {
        ShatteredSoulsSpells.LESSER_SOUL_SHARD, ShatteredSoulsSpells.SHATTERED_SOULS, ShatteredSoulsSpells.SHATTERED_SOULS_DEMON
    };

    public SpellCastResult CheckCast()
    {
        var caster = Caster;

        if (caster == null)
            return SpellCastResult.CantDoThatRightNow;

        if (!caster.GetAreaTrigger(ShatteredSoulsSpells.LESSER_SOUL_SHARD) && !caster.GetAreaTrigger(ShatteredSoulsSpells.SHATTERED_SOULS) && !caster.GetAreaTrigger(ShatteredSoulsSpells.SHATTERED_SOULS_DEMON))
            return SpellCastResult.CantDoThatRightNow;

        return SpellCastResult.SpellCastOk;
    }

    public void OnHit()
    {
        var caster = Caster;
        var target = HitUnit;

        if (caster == null || target == null)
            return;

        foreach (var spellId in _ids)

            if (TryCastDamage(caster, target, spellId))
                break;
    }

    private bool TryCastDamage(Unit caster, Unit target, uint spellId)
    {
        var at = caster.GetAreaTrigger(spellId);

        if (at != null)
        {
            caster.SpellFactory.CastSpell(target, DemonHunterSpells.SPIRIT_BOMB_DAMAGE, true);
            at.Remove();

            return true;
        }

        return false;
    }
}