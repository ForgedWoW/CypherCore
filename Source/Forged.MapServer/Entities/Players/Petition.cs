// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Framework.Database;

namespace Forged.MapServer.Entities.Players;

public class Petition
{
    public ObjectGuid OwnerGuid;
    public ObjectGuid PetitionGuid;
    public string PetitionName;
    public List<(uint AccountId, ObjectGuid PlayerGuid)> Signatures = new();
    private readonly CharacterDatabase _characterDatabase;
    private readonly ObjectAccessor _objectAccessor;

    public Petition(CharacterDatabase characterDatabase, ObjectAccessor objectAccessor)
    {
        _characterDatabase = characterDatabase;
        _objectAccessor = objectAccessor;
    }

    public void AddSignature(uint accountId, ObjectGuid playerGuid, bool isLoading)
    {
        Signatures.Add((accountId, playerGuid));

        if (isLoading)
            return;

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_PETITION_SIGNATURE);
        stmt.AddValue(0, OwnerGuid.Counter);
        stmt.AddValue(1, PetitionGuid.Counter);
        stmt.AddValue(2, playerGuid.Counter);
        stmt.AddValue(3, accountId);

        _characterDatabase.Execute(stmt);
    }

    public bool IsPetitionSignedByAccount(uint accountId)
    {
        return Signatures.Any(signature => signature.AccountId == accountId);
    }

    public void RemoveSignatureBySigner(ObjectGuid playerGuid)
    {
        foreach (var itr in Signatures)
            if (itr.PlayerGuid == playerGuid)
            {
                Signatures.Remove(itr);

                // notify owner
                var owner = _objectAccessor.FindConnectedPlayer(OwnerGuid);

                if (owner != null)
                    owner.Session.SendPetitionQuery(PetitionGuid);

                break;
            }
    }

    public void UpdateName(string newName)
    {
        PetitionName = newName;

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_PETITION_NAME);
        stmt.AddValue(0, newName);
        stmt.AddValue(1, PetitionGuid.Counter);
        _characterDatabase.Execute(stmt);
    }
}