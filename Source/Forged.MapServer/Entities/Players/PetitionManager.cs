// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Entities.Players;

public class PetitionManager
{
    private readonly CharacterDatabase _characterDatabase;
    private readonly Dictionary<ObjectGuid, Petition> _petitionStorage = new();

    public PetitionManager(CharacterDatabase characterDatabase)
    {
        _characterDatabase = characterDatabase;
    }

    public void AddPetition(ObjectGuid petitionGuid, ObjectGuid ownerGuid, string name, bool isLoading)
    {
        Petition p = new()
        {
            PetitionGuid = petitionGuid,
            OwnerGuid = ownerGuid,
            PetitionName = name
        };

        p.Signatures.Clear();

        _petitionStorage[petitionGuid] = p;

        if (isLoading)
            return;

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_PETITION);
        stmt.AddValue(0, ownerGuid.Counter);
        stmt.AddValue(1, petitionGuid.Counter);
        stmt.AddValue(2, name);
        _characterDatabase.Execute(stmt);
    }

    public Petition GetPetition(ObjectGuid petitionGuid)
    {
        return _petitionStorage.LookupByKey(petitionGuid);
    }

    public Petition GetPetitionByOwner(ObjectGuid ownerGuid)
    {
        return _petitionStorage.FirstOrDefault(p => p.Value.OwnerGuid == ownerGuid).Value;
    }

    public void LoadPetitions()
    {
        var oldMsTime = Time.MSTime;
        _petitionStorage.Clear();

        var result = _characterDatabase.Query("SELECT petitionguid, ownerguid, name FROM petition");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 petitions.");

            return;
        }

        uint count = 0;

        do
        {
            AddPetition(ObjectGuid.Create(HighGuid.Item, result.Read<ulong>(0)), ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(1)), result.Read<string>(2), true);
            ++count;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} petitions in: {Time.GetMSTimeDiffToNow(oldMsTime)} ms.");
    }

    public void LoadSignatures()
    {
        var oldMSTime = Time.MSTime;

        var result = _characterDatabase.Query("SELECT petitionguid, player_account, playerguid FROM petition_sign");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 Petition signs!");

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

        Log.Logger.Information($"Loaded {count} Petition signs in {Time.GetMSTimeDiffToNow(oldMSTime)} ms.");
    }

    public void RemovePetition(ObjectGuid petitionGuid)
    {
        _petitionStorage.Remove(petitionGuid);

        // Delete From DB
        SQLTransaction trans = new();

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_PETITION_BY_GUID);
        stmt.AddValue(0, petitionGuid.Counter);
        trans.Append(stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_PETITION_SIGNATURE_BY_GUID);
        stmt.AddValue(0, petitionGuid.Counter);
        trans.Append(stmt);

        _characterDatabase.CommitTransaction(trans);
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
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_PETITION_BY_OWNER);
        stmt.AddValue(0, ownerGuid.Counter);
        trans.Append(stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_PETITION_SIGNATURE_BY_OWNER);
        stmt.AddValue(0, ownerGuid.Counter);
        trans.Append(stmt);
        _characterDatabase.CommitTransaction(trans);
    }

    public void RemoveSignaturesBySigner(ObjectGuid signerGuid)
    {
        foreach (var petitionPair in _petitionStorage)
            petitionPair.Value.RemoveSignatureBySigner(signerGuid);

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ALL_PETITION_SIGNATURES);
        stmt.AddValue(0, signerGuid.Counter);
        _characterDatabase.Execute(stmt);
    }
}