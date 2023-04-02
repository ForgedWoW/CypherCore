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
        InviterRealm = new VirtualRealmInfo(realm.Id.GetAddress(), true, false, realm.Name, realm.NormalizedName);
    }

    public override void Write()
    {
        _worldPacket.WriteBit(CanAccept);
        _worldPacket.WriteBit(MightCRZYou);
        _worldPacket.WriteBit(IsXRealm);
        _worldPacket.WriteBit(MustBeBNetFriend);
        _worldPacket.WriteBit(AllowMultipleRoles);
        _worldPacket.WriteBit(QuestSessionActive);
        _worldPacket.WriteBits(InviterName.GetByteCount(), 6);

        InviterRealm.Write(_worldPacket);

        _worldPacket.WritePackedGuid(InviterGUID);
        _worldPacket.WritePackedGuid(InviterBNetAccountId);
        _worldPacket.WriteUInt16(Unk1);
        _worldPacket.WriteUInt32(ProposedRoles);
        _worldPacket.WriteInt32(LfgSlots.Count);
        _worldPacket.WriteInt32(LfgCompletedMask);

        _worldPacket.WriteString(InviterName);

        foreach (var LfgSlot in LfgSlots)
            _worldPacket.WriteInt32(LfgSlot);
    }
}