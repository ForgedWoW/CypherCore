// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Framework.Dynamic;

namespace Scripts.Spells.Priest;

public class DelayedAuraRemoveEvent : BasicEvent
{
    private readonly Unit _owner;
    private readonly uint _spellId;

    public DelayedAuraRemoveEvent(Unit owner, uint spellId)
    {
        _owner = owner;
        _spellId = spellId;
    }

    public override bool Execute(ulong etime, uint pTime)
    {
        _owner.RemoveAura(_spellId);

        return true;
    }
}