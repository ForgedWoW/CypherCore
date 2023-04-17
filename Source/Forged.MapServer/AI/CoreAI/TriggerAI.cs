// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Spells;

namespace Forged.MapServer.AI.CoreAI;

public class TriggerAI : NullCreatureAI
{
    public TriggerAI(Creature c) : base(c) { }

    public override void IsSummonedBy(WorldObject summoner)
    {
        if (Me.Spells[0] == 0)
            return;

        CastSpellExtraArgs extra = new()
        {
            OriginalCaster = summoner.GUID
        };

        Me.SpellFactory.CastSpell(Me, Me.Spells[0], extra);
    }
}