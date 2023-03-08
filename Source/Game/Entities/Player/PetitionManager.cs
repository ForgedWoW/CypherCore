// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Database;

namespace Game.Entities;

public class PetitionManager : Singleton<PetitionManager>
{
	readonly Dictionary<ObjectGuid, Petition> _petitionStorage = new();

	PetitionManager() { }

	public void LoadPetitions()
	{
		var oldMsTime = Time.GetMSTime();
		_petitionStorage.Clear();

		var result = DB.Characters.Query("SELECT petitionguid, ownerguid, name FROM petition");

		if (result.IsEmpty())
		{
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 petitions.");

			return;
		}

		uint count = 0;

		do
		{
			AddPetition(ObjectGuid.Create(HighGuid.Item, result.Read<ulong>(0)), ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(1)), result.Read<string>(2), true);
			++count;
		} while (result.NextRow());

		Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} petitions in: {Time.GetMSTimeDiffToNow(oldMsTime)} ms.");
	}

	public void LoadSignatures()
	{
		var oldMSTime = Time.GetMSTime();

		var result = DB.Characters.Query("SELECT petitionguid, player_account, playerguid FROM petition_sign");

		if (result.IsEmpty())
		{
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 Petition signs!");

			return;
		}

		uint count = 0;

		do
		{
			var petition = GetPetition(ObjectGuid.Create(HighGuid.Item, result.Read<ulong>(0)));

			if (petition == null)
				continue;

			petition.AddSignature(result.Read<uint>(1), ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(2)), true);
			++count;
		} while (result.NextRow());

		Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} Petition signs in {Time.GetMSTimeDiffToNow(oldMSTime)} ms.");
	}

	public void AddPetition(ObjectGuid petitionGuid, ObjectGuid ownerGuid, string name, bool isLoading)
	{
		Petition p = new();
		p.PetitionGuid = petitionGuid;
		p.OwnerGuid = ownerGuid;
		p.PetitionName = name;
		p.Signatures.Clear();

		_petitionStorage[petitionGuid] = p;

		if (isLoading)
			return;

		var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_PETITION);
		stmt.AddValue(0, ownerGuid.GetCounter());
		stmt.AddValue(1, petitionGuid.GetCounter());
		stmt.AddValue(2, name);
		DB.Characters.Execute(stmt);
	}

	public void RemovePetition(ObjectGuid petitionGuid)
	{
		_petitionStorage.Remove(petitionGuid);

		// Delete From DB
		SQLTransaction trans = new();

		var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_PETITION_BY_GUID);
		stmt.AddValue(0, petitionGuid.GetCounter());
		trans.Append(stmt);

		stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_PETITION_SIGNATURE_BY_GUID);
		stmt.AddValue(0, petitionGuid.GetCounter());
		trans.Append(stmt);

		DB.Characters.CommitTransaction(trans);
	}

	public Petition GetPetition(ObjectGuid petitionGuid)
	{
		return _petitionStorage.LookupByKey(petitionGuid);
	}

	public Petition GetPetitionByOwner(ObjectGuid ownerGuid)
	{
		return _petitionStorage.FirstOrDefault(p => p.Value.OwnerGuid == ownerGuid).Value;
	}

	public void RemovePetitionsByOwner(ObjectGuid ownerGuid)
	{
		foreach (var key in _petitionStorage.Keys.ToList())
			if (_petitionStorage[key].OwnerGuid == ownerGuid)
			{
				_petitionStorage.Remove(key);

				break;
			}

		SQLTransaction trans = new();
		var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_PETITION_BY_OWNER);
		stmt.AddValue(0, ownerGuid.GetCounter());
		trans.Append(stmt);

		stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_PETITION_SIGNATURE_BY_OWNER);
		stmt.AddValue(0, ownerGuid.GetCounter());
		trans.Append(stmt);
		DB.Characters.CommitTransaction(trans);
	}

	public void RemoveSignaturesBySigner(ObjectGuid signerGuid)
	{
		foreach (var petitionPair in _petitionStorage)
			petitionPair.Value.RemoveSignatureBySigner(signerGuid);

		var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ALL_PETITION_SIGNATURES);
		stmt.AddValue(0, signerGuid.GetCounter());
		DB.Characters.Execute(stmt);
	}
}