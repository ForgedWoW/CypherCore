// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects.Update;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Trait;

public class TraitConfigPacket
{
    public int ChrSpecializationID = 0;
    public TraitCombatConfigFlags CombatConfigFlags;
    public Dictionary<int, Dictionary<int, TraitEntryPacket>> Entries = new();
    public int ID;
    public int LocalIdentifier;
    public string Name = "";
    // Local to specialization
    public uint SkillLineID;

    public int TraitSystemID;
    public TraitConfigType Type;
    public TraitConfigPacket() { }

    public TraitConfigPacket(TraitConfig ufConfig)
    {
        ID = ufConfig.ID;
        Type = (TraitConfigType)(int)ufConfig.Type;
        ChrSpecializationID = ufConfig.ChrSpecializationID;
        CombatConfigFlags = (TraitCombatConfigFlags)(int)ufConfig.CombatConfigFlags;
        LocalIdentifier = ufConfig.LocalIdentifier;
        SkillLineID = (uint)(int)ufConfig.SkillLineID;
        TraitSystemID = ufConfig.TraitSystemID;

        foreach (var ufEntry in ufConfig.Entries)
            AddEntry(new TraitEntryPacket(ufEntry));

        Name = ufConfig.Name;
    }

    public void AddEntry(TraitEntryPacket packet)
    {
        if (!Entries.TryGetValue(packet.TraitNodeID, out var innerDict))
        {
            innerDict = new Dictionary<int, TraitEntryPacket>();
            Entries[packet.TraitNodeID] = innerDict;
        }

        innerDict[packet.TraitNodeEntryID] = packet;
    }

    public void Read(WorldPacket data)
    {
        ID = data.ReadInt32();
        Type = (TraitConfigType)data.ReadInt32();
        var entriesCount = data.ReadUInt32();

        switch (Type)
        {
            case TraitConfigType.Combat:
                ChrSpecializationID = data.ReadInt32();
                CombatConfigFlags = (TraitCombatConfigFlags)data.ReadInt32();
                LocalIdentifier = data.ReadInt32();

                break;
            case TraitConfigType.Profession:
                SkillLineID = data.ReadUInt32();

                break;
            case TraitConfigType.Generic:
                TraitSystemID = data.ReadInt32();

                break;
            
        }

        for (var i = 0; i < entriesCount; ++i)
        {
            TraitEntryPacket traitEntry = new();
            traitEntry.Read(data);
            AddEntry(traitEntry);
        }

        var nameLength = data.ReadBits<uint>(9);
        Name = data.ReadString(nameLength);
    }

    public void Write(WorldPacket data)
    {
        data.WriteInt32(ID);
        data.WriteInt32((int)Type);
        data.WriteInt32(Entries.Count);

        switch (Type)
        {
            case TraitConfigType.Combat:
                data.WriteInt32(ChrSpecializationID);
                data.WriteInt32((int)CombatConfigFlags);
                data.WriteInt32(LocalIdentifier);

                break;
            case TraitConfigType.Profession:
                data.WriteUInt32(SkillLineID);

                break;
            case TraitConfigType.Generic:
                data.WriteInt32(TraitSystemID);

                break;
            
        }

        foreach (var tkvp in Entries)
            foreach (var traitEntry in tkvp.Value.Values)
                traitEntry.Write(data);

        data.WriteBits(Name.GetByteCount(), 9);
        data.FlushBits();

        data.WriteString(Name);
    }
}