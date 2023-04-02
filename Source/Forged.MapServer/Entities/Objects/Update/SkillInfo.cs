// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;

namespace Forged.MapServer.Entities.Objects.Update;

public class SkillInfo : BaseUpdateData<Player>
{
    public UpdateFieldArray<ushort> SkillLineID = new(256, 0, 1);
    public UpdateFieldArray<ushort> SkillMaxRank = new(256, 0, 1025);
    public UpdateFieldArray<ushort> SkillPermBonus = new(256, 0, 1537);
    public UpdateFieldArray<ushort> SkillRank = new(256, 0, 513);
    public UpdateFieldArray<ushort> SkillStartingRank = new(256, 0, 769);
    public UpdateFieldArray<ushort> SkillStep = new(256, 0, 257);
    public UpdateFieldArray<ushort> SkillTempBonus = new(256, 0, 1281);
    public SkillInfo() : base(1793) { }

    public override void ClearChangesMask()
    {
        ClearChangesMask(SkillLineID);
        ClearChangesMask(SkillStep);
        ClearChangesMask(SkillRank);
        ClearChangesMask(SkillStartingRank);
        ClearChangesMask(SkillMaxRank);
        ClearChangesMask(SkillTempBonus);
        ClearChangesMask(SkillPermBonus);
        ChangesMask.ResetAll();
    }

    public void WriteCreate(WorldPacket data, Player owner, Player receiver)
    {
        for (var i = 0; i < 256; ++i)
        {
            data.WriteUInt16(SkillLineID[i]);
            data.WriteUInt16(SkillStep[i]);
            data.WriteUInt16(SkillRank[i]);
            data.WriteUInt16(SkillStartingRank[i]);
            data.WriteUInt16(SkillMaxRank[i]);
            data.WriteUInt16(SkillTempBonus[i]);
            data.WriteUInt16(SkillPermBonus[i]);
        }
    }

    public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
    {
        var changesMask = ChangesMask;

        if (ignoreChangesMask)
            changesMask.SetAll();

        for (uint i = 0; i < 1; ++i)
            data.WriteUInt32(changesMask.GetBlocksMask(i));

        data.WriteBits(changesMask.GetBlocksMask(1), 25);

        for (uint i = 0; i < 57; ++i)
            if (changesMask.GetBlock(i) != 0)
                data.WriteBits(changesMask.GetBlock(i), 32);

        data.FlushBits();

        if (changesMask[0])
            for (var i = 0; i < 256; ++i)
            {
                if (changesMask[1 + i])
                    data.WriteUInt16(SkillLineID[i]);

                if (changesMask[257 + i])
                    data.WriteUInt16(SkillStep[i]);

                if (changesMask[513 + i])
                    data.WriteUInt16(SkillRank[i]);

                if (changesMask[769 + i])
                    data.WriteUInt16(SkillStartingRank[i]);

                if (changesMask[1025 + i])
                    data.WriteUInt16(SkillMaxRank[i]);

                if (changesMask[1281 + i])
                    data.WriteUInt16(SkillTempBonus[i]);

                if (changesMask[1537 + i])
                    data.WriteUInt16(SkillPermBonus[i]);
            }
    }
}