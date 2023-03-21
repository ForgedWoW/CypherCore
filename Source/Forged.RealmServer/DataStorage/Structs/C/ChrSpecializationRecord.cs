// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.DataStorage;

public sealed class ChrSpecializationRecord
{
	public LocalizedString Name;
	public string FemaleName;
	public string Description;
	public uint Id;
	public byte ClassID;
	public byte OrderIndex;
	public sbyte PetTalentType;
	public sbyte Role;
	public ChrSpecializationFlag Flags;
	public int SpellIconFileID;
	public sbyte PrimaryStatPriority;
	public int AnimReplacements;
	public uint[] MasterySpellID = new uint[PlayerConst.MaxMasterySpells];

	public bool IsPetSpecialization()
	{
		return ClassID == 0;
	}
}