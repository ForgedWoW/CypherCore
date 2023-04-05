// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Database;

namespace Forged.MapServer.Entities.Players;

public class Petition
{
    public ObjectGuid OwnerGuid;
    public ObjectGuid PetitionGuid;
    public string PetitionName;
    public List<(uint AccountId, ObjectGuid PlayerGuid)> Signatures = new();

    public void AddSignature(uint accountId, ObjectGuid playerGuid, bool isLoading)
    {
        Signatures.Add((accountId, playerGuid));

        if (isLoading)
            return;

        var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_PETITION_SIGNATURE);
        stmt.AddValue(0, OwnerGuid.Counter);
        stmt.AddValue(1, PetitionGuid.Counter);
        stmt.AddValue(2, playerGuid.Counter);
        stmt.AddValue(3, accountId);

        CharacterDatabase.Execute(stmt);
    }

    public bool IsPetitionSignedByAccount(uint accountId)
    {
        foreach (var signature in Signatures)
            if (signature.AccountId == accountId)
                return true;

        return false;
    }
    public void RemoveSignatureBySigner(ObjectGuid playerGuid)
    {
        foreach (var itr in Signatures)
            if (itr.PlayerGuid == playerGuid)
            {
                Signatures.Remove(itr);

                // notify owner
                var owner = Global.ObjAccessor.FindConnectedPlayer(OwnerGuid);

                if (owner != null)
                    owner.Session.SendPetitionQuery(PetitionGuid);

                break;
            }
    }

    public void UpdateName(string newName)
    {
        PetitionName = newName;

        var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_PETITION_NAME);
        stmt.AddValue(0, newName);
        stmt.AddValue(1, PetitionGuid.Counter);
        CharacterDatabase.Execute(stmt);
    }
}