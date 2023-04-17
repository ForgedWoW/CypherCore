// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;
using Forged.MapServer.Spells;

namespace Scripts.Spells.Mage;

[Script] // 4658 - AreaTrigger Create Properties
internal class AreatriggerMageBlizzard : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUpdate
{
    private TimeSpan _tickTimer;

    public void OnCreate()
    {
        _tickTimer = TimeSpan.FromMilliseconds(1000);
    }

    public void OnUpdate(uint diff)
    {
        _tickTimer -= TimeSpan.FromMilliseconds(diff);

        while (_tickTimer <= TimeSpan.Zero)
        {
            var caster = At.GetCaster();

            caster?.SpellFactory.CastSpell(At.Location, MageSpells.BLIZZARD_DAMAGE, new CastSpellExtraArgs());

            _tickTimer += TimeSpan.FromMilliseconds(1000);
        }
    }
}