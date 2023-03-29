// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Spells.Auras;

public enum AuraRemoveMode
{
    None = 0,
    Default = 1, // scripted remove, remove by stack with aura with different ids and sc aura remove
    Interrupt,
    Cancel,
    EnemySpell, // dispel and absorb aura destroy
    Expire,     // aura duration has ended
    Death
}