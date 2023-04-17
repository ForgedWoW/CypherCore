// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[SpellScript(5221)]
public class SpellDruShred : SpellScript, ISpellOnHit, ISpellCalcCritChance
{
    private bool _mStealthed = false;
    private bool _mIncarnation = false;
    private uint _mCasterLevel;

    public void CalcCritChance(Unit victim, ref double chance)
    {
        // If caster is level >= 56, While stealthed or have Incarnation: King of the Jungle aura,
        // Double the chance to critically strike
        if ((_mCasterLevel >= 56) && (_mStealthed || _mIncarnation))
            chance *= 2.0f;
    }

    public override bool Load()
    {
        var caster = Caster;

        if (caster.HasAuraType(AuraType.ModStealth))
            _mStealthed = true;

        if (caster.HasAura(ShapeshiftFormSpells.IncarnationKingOfJungle))
            _mIncarnation = true;

        _mCasterLevel = caster.GetLevelForTarget(caster);

        return true;
    }

    public void OnHit()
    {
        var caster = Caster;
        var target = HitUnit;

        if (caster == null || target == null)
            return;

        var damage = HitDamage;

        caster.ModifyPower(PowerType.ComboPoints, 1);

        // If caster is level >= 56, While stealthed or have Incarnation: King of the Jungle aura,
        // deals 50% increased damage (get value from the spell data)
        if ((caster.HasAura(231057)) && (_mStealthed || _mIncarnation))
            MathFunctions.AddPct(ref damage, Global.SpellMgr.GetSpellInfo(DruidSpells.Shred, Difficulty.None).GetEffect(2).BasePoints);

        HitDamage = damage;
    }
}