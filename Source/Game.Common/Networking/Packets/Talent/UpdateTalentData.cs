// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Talent;

namespace Game.Common.Networking.Packets.Talent;

public class UpdateTalentData : ServerPacket
{
	public TalentInfoUpdate Info = new();
	public UpdateTalentData() : base(ServerOpcodes.UpdateTalentData, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt8(Info.ActiveGroup);
		_worldPacket.WriteUInt32(Info.PrimarySpecialization);
		_worldPacket.WriteInt32(Info.TalentGroups.Count);

		foreach (var talentGroupInfo in Info.TalentGroups)
		{
			_worldPacket.WriteUInt32(talentGroupInfo.SpecID);
			_worldPacket.WriteInt32(talentGroupInfo.TalentIDs.Count);
			_worldPacket.WriteInt32(talentGroupInfo.PvPTalents.Count);

			foreach (var talentID in talentGroupInfo.TalentIDs)
				_worldPacket.WriteUInt16(talentID);

			foreach (var talent in talentGroupInfo.PvPTalents)
				talent.Write(_worldPacket);
		}
	}

	public class TalentGroupInfo
	{
		public uint SpecID;
		public List<ushort> TalentIDs = new();
		public List<PvPTalent> PvPTalents = new();
	}

	public class TalentInfoUpdate
	{
		public byte ActiveGroup;
		public uint PrimarySpecialization;
		public List<TalentGroupInfo> TalentGroups = new();
	}
}

//Structs