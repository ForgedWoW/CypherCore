// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Spells.Auras;
using Framework.Dynamic;

namespace Forged.MapServer.Spells;

public class SpellModifierByClassMask : SpellModifier
{
    public double Value;
    public FlagArray128 Mask;

    public SpellModifierByClassMask(Aura ownerAura) : base(ownerAura)
    {
        Value = 0;
        Mask = new FlagArray128();
    }
}