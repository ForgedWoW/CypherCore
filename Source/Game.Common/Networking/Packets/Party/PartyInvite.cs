// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class PartyInvite : ServerPacket
{
	public bool MightCRZYou;
	public bool MustBeBNetFriend;
	public bool AllowMultipleRoles;
	public bool QuestSessionActive;
	public ushort Unk1;

	public bool CanAccept;

	// Inviter
	public VirtualRealmInfo InviterRealm;
	public ObjectGuid InviterGUID;
	public ObjectGuid InviterBNetAccountId;
	public string InviterName;

	// Realm
	public bool IsXRealm;

	// Lfg
	public uint ProposedRoles;
	public int LfgCompletedMask;
	public List<int> LfgSlots = new();
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