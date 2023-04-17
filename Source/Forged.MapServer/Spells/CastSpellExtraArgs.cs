// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Forged.MapServer.Spells;

public class CastSpellExtraArgs
{
    public CastSpellExtraArgs() { }

    public CastSpellExtraArgs(bool triggered)
    {
        TriggerFlags = triggered ? TriggerCastFlags.FullMask : TriggerCastFlags.None;
    }

    public CastSpellExtraArgs(TriggerCastFlags trigger)
    {
        TriggerFlags = trigger;
    }

    public CastSpellExtraArgs(Item item)
    {
        TriggerFlags = TriggerCastFlags.FullMask;
        CastItem = item;
    }

    public CastSpellExtraArgs(Spell triggeringSpell)
    {
        TriggerFlags = TriggerCastFlags.FullMask;
        SetTriggeringSpell(triggeringSpell);
    }

    public CastSpellExtraArgs(AuraEffect eff)
    {
        TriggerFlags = TriggerCastFlags.FullMask;
        SetTriggeringAura(eff);
    }

    public CastSpellExtraArgs(Difficulty castDifficulty)
    {
        CastDifficulty = castDifficulty;
    }

    public CastSpellExtraArgs(SpellValueMod mod, double val)
    {
        SpellValueOverrides.Add(mod, val);
    }

    public Difficulty CastDifficulty { get; set; }
    public Item CastItem { get; set; }
    public object CustomArg { get; set; }
    public byte? EmpowerStage { get; set; }
    public ObjectGuid OriginalCaster { get; set; } = ObjectGuid.Empty;
    public ObjectGuid OriginalCastId { get; set; } = ObjectGuid.Empty;
    public int? OriginalCastItemLevel { get; set; }
    public Dictionary<SpellValueMod, double> SpellValueOverrides { get; set; } = new();
    public TriggerCastFlags TriggerFlags { get; set; }
    public AuraEffect TriggeringAura { get; set; }
    public Spell TriggeringSpell { get; set; }

    public CastSpellExtraArgs AddSpellMod(SpellValueMod mod, double val)
    {
        SpellValueOverrides[mod] = val;

        return this;
    }

    public CastSpellExtraArgs SetCastDifficulty(Difficulty castDifficulty)
    {
        CastDifficulty = castDifficulty;

        return this;
    }

    public CastSpellExtraArgs SetCastItem(Item item)
    {
        CastItem = item;

        return this;
    }

    public CastSpellExtraArgs SetCustomArg(object customArg)
    {
        CustomArg = customArg;

        return this;
    }

    public CastSpellExtraArgs SetIsTriggered(bool triggered)
    {
        TriggerFlags = triggered ? TriggerCastFlags.FullMask : TriggerCastFlags.None;

        return this;
    }

    public CastSpellExtraArgs SetOriginalCaster(ObjectGuid guid)
    {
        OriginalCaster = guid;

        return this;
    }

    public CastSpellExtraArgs SetOriginalCastId(ObjectGuid castId)
    {
        OriginalCastId = castId;

        return this;
    }

    public CastSpellExtraArgs SetSpellValueMod(SpellValueMod mod, double val)
    {
        SpellValueOverrides[mod] = val;

        return this;
    }

    public CastSpellExtraArgs SetTriggerFlags(TriggerCastFlags flag)
    {
        TriggerFlags = flag;

        return this;
    }

    public CastSpellExtraArgs SetTriggeringAura(AuraEffect triggeringAura)
    {
        TriggeringAura = triggeringAura;

        if (triggeringAura != null)
            OriginalCastId = triggeringAura.Base.CastId;

        return this;
    }

    public CastSpellExtraArgs SetTriggeringSpell(Spell triggeringSpell)
    {
        TriggeringSpell = triggeringSpell;

        if (triggeringSpell == null)
            return this;

        OriginalCastItemLevel = triggeringSpell.CastItemLevel;
        OriginalCastId = triggeringSpell.CastId;

        return this;
    }
}