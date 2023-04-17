// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Rogue;

[SpellScript(193537)]
public class SpellRogWeaponmasterAuraScript : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var caster = eventInfo.Actor;
        var target = eventInfo.ActionTarget;

        if (target == null || caster == null)
            return false;

        var triggerSpell = eventInfo.SpellInfo;

        if (triggerSpell == null)
            return false;

        if (!RandomHelper.randChance(6))
            return false;

        if (eventInfo.DamageInfo != null)
            return false;

        var damageLog = new SpellNonMeleeDamage(caster, target, triggerSpell, new SpellCastVisual(triggerSpell.GetSpellXSpellVisualId(), 0), triggerSpell.SchoolMask);
        damageLog.Damage = eventInfo.DamageInfo.Damage;
        damageLog.CleanDamage = damageLog.Damage;
        caster.DealSpellDamage(damageLog, true);
        caster.SendSpellNonMeleeDamageLog(damageLog);

        return true;
    }
}