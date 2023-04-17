// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Spells;

public class ImmunityInfo
{
    public uint ApplyHarmfulAuraImmuneMask { get; set; }
    public List<AuraType> AuraTypeImmune { get; set; } = new();
    public uint DamageSchoolMask { get; set; }
    public uint DispelImmune { get; set; }
    public ulong MechanicImmuneMask { get; set; }
    public uint SchoolImmuneMask { get; set; }
    public List<SpellEffectName> SpellEffectImmune { get; set; } = new();
}