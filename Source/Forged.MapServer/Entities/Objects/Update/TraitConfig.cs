// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;

namespace Forged.MapServer.Entities.Objects.Update;

public class TraitConfig : BaseUpdateData<Player>
{
    public DynamicUpdateField<TraitEntry> Entries = new(0, 1);
    public UpdateField<int> ID = new(0, 2);
    public UpdateFieldString Name = new(0, 3);
    public UpdateField<int> Type = new(4, 5);
    public UpdateField<int> SkillLineID = new(4, 6);
    public UpdateField<int> ChrSpecializationID = new(4, 7);
    public UpdateField<int> CombatConfigFlags = new(8, 9);
    public UpdateField<int> LocalIdentifier = new(8, 10);
    public UpdateField<int> TraitSystemID = new(8, 11);

    public TraitConfig() : base(12) { }

    public void WriteCreate(WorldPacket data, Player owner, Player receiver)
    {
        data.WriteInt32(ID);
        data.WriteInt32(Type);
        data.WriteInt32(Entries.Size());

        if (Type == 2)
            data.WriteInt32(SkillLineID);

        if (Type == 1)
        {
            data.WriteInt32(ChrSpecializationID);
            data.WriteInt32(CombatConfigFlags);
            data.WriteInt32(LocalIdentifier);
        }

        if (Type == 3)
            data.WriteInt32(TraitSystemID);

        for (var i = 0; i < Entries.Size(); ++i)
            Entries[i].WriteCreate(data, owner, receiver);

        data.WriteBits(Name.GetValue().GetByteCount(), 9);
        data.WriteString(Name);
        data.FlushBits();
    }

    public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
    {
        var changesMask = ChangesMask;

        if (ignoreChangesMask)
            changesMask.SetAll();

        data.WriteBits(changesMask.GetBlock(0), 12);

        if (changesMask[0])
            if (changesMask[1])
            {
                if (!ignoreChangesMask)
                    Entries.WriteUpdateMask(data);
                else
                    WriteCompleteDynamicFieldUpdateMask(Entries.Size(), data);
            }

        data.FlushBits();

        if (changesMask[0])
        {
            if (changesMask[1])
                for (var i = 0; i < Entries.Size(); ++i)
                    if (Entries.HasChanged(i) || ignoreChangesMask)
                        Entries[i].WriteUpdate(data, ignoreChangesMask, owner, receiver);

            if (changesMask[2])
                data.WriteInt32(ID);
        }

        if (changesMask[4])
        {
            if (changesMask[5])
                data.WriteInt32(Type);

            if (changesMask[6])
                if (Type == 2)
                    data.WriteInt32(SkillLineID);

            if (changesMask[7])
                if (Type == 1)
                    data.WriteInt32(ChrSpecializationID);
        }

        if (changesMask[8])
        {
            if (changesMask[9])
                if (Type == 1)
                    data.WriteInt32(CombatConfigFlags);

            if (changesMask[10])
                if (Type == 1)
                    data.WriteInt32(LocalIdentifier);

            if (changesMask[11])
                if (Type == 3)
                    data.WriteInt32(TraitSystemID);
        }

        if (changesMask[0])
            if (changesMask[3])
            {
                data.WriteBits(Name.GetValue().GetByteCount(), 9);
                data.WriteString(Name);
            }

        data.FlushBits();
    }

    public override void ClearChangesMask()
    {
        ClearChangesMask(Entries);
        ClearChangesMask(ID);
        ClearChangesMask(Name);
        ClearChangesMask(Type);
        ClearChangesMask(SkillLineID);
        ClearChangesMask(ChrSpecializationID);
        ClearChangesMask(CombatConfigFlags);
        ClearChangesMask(LocalIdentifier);
        ClearChangesMask(TraitSystemID);
        ChangesMask.ResetAll();
    }
}