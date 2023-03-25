// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.DataStorage;

public sealed class BattlePetSpeciesRecord
{
	public string Description;
	public string SourceText;
	public uint Id;
	public uint CreatureID;
	public uint SummonSpellID;
	public int IconFileDataID;
	public sbyte PetTypeEnum;
	public int Flags;
	public sbyte SourceTypeEnum;
	public int CardUIModelSceneID;
	public int LoadoutUIModelSceneID;
	public int CovenantID;

	public BattlePetSpeciesFlags GetFlags()
	{
		return (BattlePetSpeciesFlags)Flags;
	}
}