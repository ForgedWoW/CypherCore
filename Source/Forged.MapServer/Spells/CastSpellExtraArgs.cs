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
    public Difficulty CastDifficulty;
    public Item CastItem;
    public object CustomArg;
    public byte? EmpowerStage;
    public ObjectGuid OriginalCaster = ObjectGuid.Empty;
    public ObjectGuid OriginalCastId = ObjectGuid.Empty;
    public int? OriginalCastItemLevel;
    public Dictionary<SpellValueMod, double> SpellValueOverrides = new();
    public TriggerCastFlags TriggerFlags;
    public AuraEffect TriggeringAura;
    public Spell TriggeringSpell;
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

        if (triggeringSpell != null)
        {
            OriginalCastItemLevel = triggeringSpell.CastItemLevel;
            OriginalCastId = triggeringSpell.CastId;
        }

        return this;
    }
}