// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[Script]
public class SpellDkCommanderOfTheDeadAura : SpellScript, IHasSpellEffects
{
    private readonly List<WorldObject> _saveTargets = new();
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.Launch));
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(GetListOfUnits, 0, Targets.UnitCasterAndSummons));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster;
        var target = HitUnit;

        if (caster != null)
            if (_saveTargets.Count > 0)
            {
                _saveTargets.ForEach(k => { caster.SpellFactory.CastSpell(k, DeathKnightSpells.DT_COMMANDER_BUFF, true); });
                _saveTargets.Clear();
            }
    }

    private void GetListOfUnits(List<WorldObject> targets)
    {
        targets.RemoveIf((WorldObject target) =>
        {
            if (!target.AsUnit || target.AsPlayer)
                return true;

            if (target.AsCreature.OwnerUnit != Caster)
                return true;

            if (target.AsCreature.Entry != DeathKnightSpells.Dknpcs.GARGOYLE && target.AsCreature.Entry != DeathKnightSpells.Dknpcs.AOTD_GHOUL)
                return true;

            _saveTargets.Add(target);

            return false;
        });

        targets.Clear();
    }
}

[SpellScript(390259)]
public class SpellDkCommanderOfTheDeadAuraProc : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public bool CheckProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.SpellInfo != null)
            return eventInfo.SpellInfo.Id == DeathKnightSpells.DARK_TRANSFORMATION;

        return false;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        Target.SpellFactory.CastSpell(eventInfo.ProcTarget, DeathKnightSpells.DT_COMMANDER_BUFF, true);
    }
}