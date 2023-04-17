// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Networking.Packets.MythicPlus;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Party;

internal class PartyMemberStats
{
    public List<PartyMemberAuraStates> Auras = new();
    public CTROptions ChromieTime;
    public int CurrentHealth;
    public ushort CurrentPower;
    public DungeonScoreSummary DungeonScore = new();
    public ushort Level;
    public int MaxHealth;
    public ushort MaxPower;
    public sbyte[] PartyType = new sbyte[2];
    public PartyMemberPetStats PetStats;
    public PartyMemberPhaseStates Phases = new();
    public short PositionX;
    public short PositionY;
    public short PositionZ;
    public ushort PowerDisplayID;
    public byte PowerType;
    public ushort SpecID;
    public GroupMemberOnlineStatus Status;
    public int VehicleSeat;
    public uint WmoDoodadPlacementID;
    public ushort WmoGroupID;
    public ushort ZoneID;

    public void Write(WorldPacket data)
    {
        for (byte i = 0; i < 2; i++)
            data.WriteInt8(PartyType[i]);

        data.WriteInt16((short)Status);
        data.WriteUInt8(PowerType);
        data.WriteInt16((short)PowerDisplayID);
        data.WriteInt32(CurrentHealth);
        data.WriteInt32(MaxHealth);
        data.WriteUInt16(CurrentPower);
        data.WriteUInt16(MaxPower);
        data.WriteUInt16(Level);
        data.WriteUInt16(SpecID);
        data.WriteUInt16(ZoneID);
        data.WriteUInt16(WmoGroupID);
        data.WriteUInt32(WmoDoodadPlacementID);
        data.WriteInt16(PositionX);
        data.WriteInt16(PositionY);
        data.WriteInt16(PositionZ);
        data.WriteInt32(VehicleSeat);
        data.WriteInt32(Auras.Count);

        Phases.Write(data);
        ChromieTime.Write(data);

        foreach (var aura in Auras)
            aura.Write(data);

        data.WriteBit(PetStats != null);
        data.FlushBits();

        DungeonScore.Write(data);

        PetStats?.Write(data);
    }
}