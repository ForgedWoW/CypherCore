// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Autofac;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Spells;

public class SpellFactory
{
    private readonly WorldObject _caster;
    private readonly ClassFactory _classFactory;
    private readonly SpellManager _spellManager;
    public SpellFactory(WorldObject caster, ClassFactory classFactory, SpellManager spellManager)
    {
        _classFactory = classFactory;
        _spellManager = spellManager;
        _caster = caster;
    }

    public SpellCastResult CastSpell(uint spellId, bool triggered = false, byte? empowerStage = null)
    {
        return CastSpell(null, spellId, triggered, empowerStage);
    }

    public SpellCastResult CastSpell<T>(WorldObject target, T spellId, bool triggered = false) where T : struct, Enum
    {
        return CastSpell(target, Convert.ToUInt32(spellId), triggered);
    }

    public SpellCastResult CastSpell(WorldObject target, uint spellId, Spell triggeringSpell)
    {
        CastSpellExtraArgs args = new(true)
        {
            TriggeringSpell = triggeringSpell
        };

        return CastSpell(target, spellId, args);
    }

    public SpellCastResult CastSpell(WorldObject target, uint spellId, AuraEffect triggeringAura)
    {
        CastSpellExtraArgs args = new(true)
        {
            TriggeringAura = triggeringAura
        };

        return CastSpell(target, spellId, args);
    }

    public SpellCastResult CastSpell(WorldObject target, uint spellId, bool triggered = false, byte? empowerStage = null)
    {
        CastSpellExtraArgs args = new(triggered)
        {
            EmpowerStage = empowerStage
        };

        return CastSpell(target, spellId, args);
    }

    public SpellCastResult CastSpell(WorldObject target, uint spellId, TriggerCastFlags triggerCastFlags, bool triggered = false)
    {
        CastSpellExtraArgs args = new(triggered)
        {
            TriggerFlags = triggerCastFlags
        };

        return CastSpell(target, spellId, args);
    }

    public SpellCastResult CastSpell(WorldObject target, uint spellId, double bp0Val, bool triggered = false)
    {
        CastSpellExtraArgs args = new(triggered)
        {
            SpellValueOverrides =
            {
                [SpellValueMod.BasePoint0] = bp0Val
            }
        };

        return CastSpell(target, spellId, args);
    }

    public SpellCastResult CastSpell(WorldObject target, uint spellId, SpellValueMod spellValueMod, double bp0Val, bool triggered = false)
    {
        CastSpellExtraArgs args = new(triggered)
        {
            SpellValueOverrides =
            {
                [spellValueMod] = bp0Val
            }
        };

        return CastSpell(target, spellId, args);
    }

    public SpellCastResult CastSpell(SpellCastTargets targets, uint spellId, CastSpellExtraArgs args)
    {
        return CastSpell(new CastSpellTargetArg(targets), spellId, args);
    }

    public SpellCastResult CastSpell(WorldObject target, uint spellId, CastSpellExtraArgs args)
    {
        return CastSpell(new CastSpellTargetArg(target), spellId, args);
    }

    public SpellCastResult CastSpell(float x, float y, float z, uint spellId, bool triggered = false)
    {
        return CastSpell(new Position(x, y, z), spellId, triggered);
    }

    public SpellCastResult CastSpell(float x, float y, float z, uint spellId, CastSpellExtraArgs args)
    {
        return CastSpell(new Position(x, y, z), spellId, args);
    }

    public SpellCastResult CastSpell(Position dest, uint spellId, bool triggered = false)
    {
        CastSpellExtraArgs args = new(triggered);

        return CastSpell(new CastSpellTargetArg(dest), spellId, args);
    }

    public SpellCastResult CastSpell(Position dest, uint spellId, CastSpellExtraArgs args)
    {
        return CastSpell(new CastSpellTargetArg(dest), spellId, args);
    }

    public SpellCastResult CastSpell(CastSpellTargetArg targets, uint spellId, CastSpellExtraArgs args)
    {
        var info = _spellManager.GetSpellInfo(spellId, args.CastDifficulty != Difficulty.None ? args.CastDifficulty : _caster.Location.Map.DifficultyID);

        if (info == null)
        {
            Log.Logger.Error($"CastSpell: unknown spell {spellId} by caster {_caster.GUID}");

            return SpellCastResult.SpellUnavailable;
        }

        if (targets.Targets == null)
        {
            Log.Logger.Error($"CastSpell: Invalid target passed to spell cast {spellId} by {_caster.GUID}");

            return SpellCastResult.BadTargets;
        }

        var spell = NewSpell(info, args.TriggerFlags, args.OriginalCaster, args.OriginalCastId, args.EmpowerStage);

        foreach (var pair in args.SpellValueOverrides)
            spell.SetSpellValue(pair.Key, (float)pair.Value);

        spell.CastItem = args.CastItem;

        if (args.OriginalCastItemLevel.HasValue)
            spell.CastItemLevel = args.OriginalCastItemLevel.Value;

        if (spell.CastItem == null && info.HasAttribute(SpellAttr2.RetainItemCast))
        {
            if (args.TriggeringSpell)
            {
                spell.CastItem = args.TriggeringSpell.CastItem;
            }
            else if (args.TriggeringAura != null && !args.TriggeringAura.Base.CastItemGuid.IsEmpty)
            {
                var triggeringAuraCaster = args.TriggeringAura.Caster?.AsPlayer;

                if (triggeringAuraCaster != null)
                    spell.CastItem = triggeringAuraCaster.GetItemByGuid(args.TriggeringAura.Base.CastItemGuid);
            }
        }

        spell.CustomArg = args.CustomArg;

        return spell.Prepare(targets.Targets, args.TriggeringAura);
    }

    public Spell NewSpell(SpellInfo info, TriggerCastFlags triggerFlags, ObjectGuid originalCasterGuid = default, ObjectGuid originalCastId = default, byte? empoweredStage = null)
    {
        return _classFactory.Resolve<Spell>(new PositionalParameter(0, _caster),
                                            new PositionalParameter(1, info),
                                            new PositionalParameter(2, triggerFlags),
                                            new NamedParameter(nameof(originalCasterGuid), originalCasterGuid),
                                            new NamedParameter(nameof(originalCastId), originalCastId),
                                            new NamedParameter(nameof(empoweredStage), empoweredStage));
    }
}