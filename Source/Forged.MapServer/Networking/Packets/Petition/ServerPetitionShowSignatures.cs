// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Petition;

public class ServerPetitionShowSignatures : ServerPacket
{
    public ObjectGuid Item;
    public ObjectGuid Owner;
    public ObjectGuid OwnerAccountID;
    public int PetitionID = 0;
    public List<PetitionSignature> Signatures;

    public ServerPetitionShowSignatures() : base(ServerOpcodes.PetitionShowSignatures)
    {
        Signatures = new List<PetitionSignature>();
    }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(Item);
        _worldPacket.WritePackedGuid(Owner);
        _worldPacket.WritePackedGuid(OwnerAccountID);
        _worldPacket.WriteInt32(PetitionID);

        _worldPacket.WriteInt32(Signatures.Count);

        foreach (var signature in Signatures)
        {
            _worldPacket.WritePackedGuid(signature.Signer);
            _worldPacket.WriteInt32(signature.Choice);
        }
    }

    public struct PetitionSignature
    {
        public int Choice;
        public ObjectGuid Signer;
    }
}