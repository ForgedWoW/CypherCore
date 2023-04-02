// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Spells;

public class ImmunityInfo
{
    public uint ApplyHarmfulAuraImmuneMask;
    public List<AuraType> AuraTypeImmune = new();
    public uint DamageSchoolMask;
    public uint DispelImmune;
    public ulong MechanicImmuneMask;
    public uint SchoolImmuneMask;
    public List<SpellEffectName> SpellEffectImmune = new();
}