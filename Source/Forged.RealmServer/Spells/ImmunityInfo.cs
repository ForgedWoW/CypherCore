// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.RealmServer.Spells;

public class ImmunityInfo
{
	public uint SchoolImmuneMask;
	public uint ApplyHarmfulAuraImmuneMask;
	public ulong MechanicImmuneMask;
	public uint DispelImmune;
	public uint DamageSchoolMask;

	public List<AuraType> AuraTypeImmune = new();
	public List<SpellEffectName> SpellEffectImmune = new();
}