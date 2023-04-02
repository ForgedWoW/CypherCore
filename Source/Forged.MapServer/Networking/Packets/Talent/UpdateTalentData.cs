// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Talent;

internal class UpdateTalentData : ServerPacket
{
    public TalentInfoUpdate Info = new();
    public UpdateTalentData() : base(ServerOpcodes.UpdateTalentData, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteUInt8(Info.ActiveGroup);
        WorldPacket.WriteUInt32(Info.PrimarySpecialization);
        WorldPacket.WriteInt32(Info.TalentGroups.Count);

        foreach (var talentGroupInfo in Info.TalentGroups)
        {
            WorldPacket.WriteUInt32(talentGroupInfo.SpecID);
            WorldPacket.WriteInt32(talentGroupInfo.TalentIDs.Count);
            WorldPacket.WriteInt32(talentGroupInfo.PvPTalents.Count);

            foreach (var talentID in talentGroupInfo.TalentIDs)
                WorldPacket.WriteUInt16(talentID);

            foreach (var talent in talentGroupInfo.PvPTalents)
                talent.Write(WorldPacket);
        }
    }

    public class TalentGroupInfo
    {
        public List<PvPTalent> PvPTalents = new();
        public uint SpecID;
        public List<ushort> TalentIDs = new();
    }

    public class TalentInfoUpdate
    {
        public byte ActiveGroup;
        public uint PrimarySpecialization;
        public List<TalentGroupInfo> TalentGroups = new();
    }
}

//Structs