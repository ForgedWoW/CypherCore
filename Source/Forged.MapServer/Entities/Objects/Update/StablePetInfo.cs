// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;

namespace Forged.MapServer.Entities.Objects.Update;

public class StablePetInfo : BaseUpdateData<StablePetInfo>
{
    public UpdateField<uint> PetSlot = new(0, 1);
    public UpdateField<uint> PetNumber = new(0, 2);
    public UpdateField<uint> CreatureID = new(0, 3);
    public UpdateField<uint> DisplayID = new(0, 4);
    public UpdateField<uint> ExperienceLevel = new(0, 5);
    public UpdateFieldString Name = new(0, 6);
    public UpdateField<byte> PetFlags = new(0, 7);

    public StablePetInfo()  : base(8)
    {

    }

    public void WriteCreate(WorldPacket data, Player owner, Player receiver)
    {
        data.WriteUInt32(PetSlot);
        data.WriteUInt32(PetNumber);
        data.WriteUInt32(CreatureID);
        data.WriteUInt32(DisplayID);
        data.WriteUInt32(ExperienceLevel);
        data.WriteUInt8(PetFlags);
        data.WriteBits(Name.Value.Length, 8);
        data.WriteString(Name);
        data.FlushBits();
    }

    public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
    {
        var changesMask = ChangesMask;

        if (ignoreChangesMask)
            changesMask.SetAll();

        data.WriteBits(changesMask.GetBlock(0), 8);
        data.FlushBits();

        if (changesMask[0])
        {
            if (changesMask[1])
            {
                data.WriteUInt32(PetSlot);
            }
            if (changesMask[2])
            {
                data.WriteUInt32(PetNumber);
            }
            if (changesMask[3])
            {
                data.WriteUInt32(CreatureID);
            }
            if (changesMask[4])
            {
                data.WriteUInt32(DisplayID);
            }
            if (changesMask[5])
            {
                data.WriteUInt32(ExperienceLevel);
            }
            if (changesMask[7])
            {
                data.WriteUInt8(PetFlags);
            }
            if (changesMask[6])
            {
                data.WriteBits(Name.Value.Length, 8);
                data.WriteString(Name);
            }
        }

        data.FlushBits();
    }

    public override void ClearChangesMask()
    {
        ClearChangesMask(PetSlot);
        ClearChangesMask(PetNumber);
        ClearChangesMask(CreatureID);
        ClearChangesMask(DisplayID);
        ClearChangesMask(ExperienceLevel);
        ClearChangesMask(Name);
        ClearChangesMask(PetFlags);
        ChangesMask.ResetAll();
    }
}