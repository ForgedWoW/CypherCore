// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

[Script]
public class AtDemonHunterManaRift : AreaTriggerScript, IAreaTriggerOnUnitExit
{
    public void OnUnitExit(Unit unit)
    {
        var caster = At.GetCaster();

        if (caster == null || unit == null)
            return;

        var spellProto = Global.SpellMgr.GetSpellInfo(DemonHunterSpells.MANA_RIFT_SPELL, Difficulty.None);

        if (spellProto == null)
            return;

        if (At.IsRemoved)
            if (caster.IsValidAttackTarget(unit))
            {
                var hpBp = unit.CountPctFromMaxHealth(spellProto.GetEffect(1).BasePoints);
                var manaBp = unit.CountPctFromMaxPower(PowerType.Mana, spellProto.GetEffect(2).BasePoints);
                var args = new CastSpellExtraArgs();
                args.AddSpellMod(SpellValueMod.BasePoint0, hpBp);
                args.AddSpellMod(SpellValueMod.BasePoint0, manaBp);
                args.SetTriggerFlags(TriggerCastFlags.FullMask);
                caster.SpellFactory.CastSpell(unit, DemonHunterSpells.MANA_RIFT_DAMAGE, args);
            }
    }
}