// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

//187880 - Maelstrom Weapon
[SpellScript(187880)]
public class SpellShaMaelstromWeapon : AuraScript, IHasAuraEffects, IAuraCheckProc
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public bool CheckProc(ProcEventInfo info)
    {
        return info.DamageInfo.AttackType == WeaponAttackType.BaseAttack || info.DamageInfo.AttackType == WeaponAttackType.OffAttack || info.SpellInfo.Id == ShamanSpells.WINDFURY_ATTACK;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    public void HandleEffectProc(AuraEffect unnamedParameter, ProcEventInfo unnamedParameter2)
    {
        var caster = Caster;

        if (caster != null)
            caster.SpellFactory.CastSpell(caster, ShamanSpells.MAELSTROM_WEAPON_POWER, true);
    }
}