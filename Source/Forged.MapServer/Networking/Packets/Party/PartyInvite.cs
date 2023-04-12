// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking.Packets.Authentication;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Party;

internal class PartyInvite : ServerPacket
{
    public bool AllowMultipleRoles;
    public bool CanAccept;
    public ObjectGuid InviterBNetAccountId;
    public ObjectGuid InviterGUID;
    public string InviterName;
    // Inviter
    public VirtualRealmInfo InviterRealm;

    // Realm
    public bool IsXRealm;

    public int LfgCompletedMask;
    public List<int> LfgSlots = new();
    public bool MightCRZYou;
    public bool MustBeBNetFriend;
    // Lfg
    public uint ProposedRoles;

    public bool QuestSessionActive;
    public ushort Unk1;
    public PartyInvite() : base(ServerOpcodes.PartyInvite) { }

    public void Initialize(Player inviter, uint proposedRoles, bool canAccept)
    {
        CanAccept = canAccept;

        InviterName = inviter.GetName();
        InviterGUID = inviter.GUID;
        InviterBNetAccountId = inviter.Session.AccountGUID;

        ProposedRoles = proposedRoles;

        var realm = Global.WorldMgr.Realm;
        InviterRealm = new VirtualRealmInfo(Realm.Id.VirtualRealmAddress, true, false, realm.Name, realm.NormalizedName);
    }

    public override void Write()
    {
        WorldPacket.WriteBit(CanAccept);
        WorldPacket.WriteBit(MightCRZYou);
        WorldPacket.WriteBit(IsXRealm);
        WorldPacket.WriteBit(MustBeBNetFriend);
        WorldPacket.WriteBit(AllowMultipleRoles);
        WorldPacket.WriteBit(QuestSessionActive);
        WorldPacket.WriteBits(InviterName.GetByteCount(), 6);

        InviterRealm.Write(WorldPacket);

        WorldPacket.WritePackedGuid(InviterGUID);
        WorldPacket.WritePackedGuid(InviterBNetAccountId);
        WorldPacket.WriteUInt16(Unk1);
        WorldPacket.WriteUInt32(ProposedRoles);
        WorldPacket.WriteInt32(LfgSlots.Count);
        WorldPacket.WriteInt32(LfgCompletedMask);

        WorldPacket.WriteString(InviterName);

        foreach (var LfgSlot in LfgSlots)
            WorldPacket.WriteInt32(LfgSlot);
    }
}