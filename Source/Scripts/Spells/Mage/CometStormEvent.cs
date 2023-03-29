// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;
using Game.Spells;

namespace Scripts.Spells.Mage;

internal class CometStormEvent : BasicEvent
{
    private readonly Unit _caster;
    private readonly Position _dest;
    private readonly ObjectGuid _originalCastId;
    private byte _count;

    public CometStormEvent(Unit caster, ObjectGuid originalCastId, Position dest)
    {
        _caster = caster;
        _originalCastId = originalCastId;
        _dest = dest;
    }

    public override bool Execute(ulong etime, uint pTime)
    {
        Position destPosition = new(_dest.X + RandomHelper.FRand(-3.0f, 3.0f), _dest.Y + RandomHelper.FRand(-3.0f, 3.0f), _dest.Z);
        _caster.CastSpell(destPosition, MageSpells.CometStormVisual, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress).SetOriginalCastId(_originalCastId));
        ++_count;

        if (_count >= 7)
            return true;

        _caster.Events.AddEvent(this, TimeSpan.FromMilliseconds(etime) + RandomHelper.RandTime(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(275)));

        return false;
    }
}