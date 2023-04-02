// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Pet;

public class PetSpellCooldown
{
    public ushort Category;
    public uint CategoryDuration;
    public uint Duration;
    public float ModRate = 1.0f;
    public uint SpellID;
}