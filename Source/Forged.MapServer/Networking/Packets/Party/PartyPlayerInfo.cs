// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Party;

internal struct PartyPlayerInfo
{
	public void Write(WorldPacket data)
	{
		data.WriteBits(Name.GetByteCount(), 6);
		data.WriteBits(VoiceStateID.GetByteCount() + 1, 6);
		data.WriteBit(Connected);
		data.WriteBit(VoiceChatSilenced);
		data.WriteBit(FromSocialQueue);
		data.WritePackedGuid(GUID);
		data.WriteUInt8(Subgroup);
		data.WriteUInt8(Flags);
		data.WriteUInt8(RolesAssigned);
		data.WriteUInt8(Class);
		data.WriteUInt8(FactionGroup);
		data.WriteString(Name);

		if (!VoiceStateID.IsEmpty())
			data.WriteString(VoiceStateID);
	}

	public ObjectGuid GUID;
	public string Name;
	public string VoiceStateID; // same as bgs.protocol.club.v1.MemberVoiceState.id
	public byte Class;
	public byte Subgroup;
	public byte Flags;
	public byte RolesAssigned;
	public byte FactionGroup;
	public bool FromSocialQueue;
	public bool VoiceChatSilenced;
	public bool Connected;
}