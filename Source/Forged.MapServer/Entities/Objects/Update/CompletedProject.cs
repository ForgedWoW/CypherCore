// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;

namespace Forged.MapServer.Entities.Objects.Update;

public class CompletedProject : BaseUpdateData<CompletedProject>
{
    public UpdateField<uint> ProjectID = new(0, 1);
    public UpdateField<long> FirstCompleted = new(0, 2);
    public UpdateField<uint> CompletionCount = new(0, 3);

    public CompletedProject()  : base(4)
    {

    }

    public void WriteCreate(WorldPacket data, Player owner, Player receiver)
    {
        data.WriteUInt32(ProjectID);
        data.WriteInt64(FirstCompleted);
        data.WriteUInt32(CompletionCount);
    }

    public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
    {
        var changesMask = ChangesMask;

        if (ignoreChangesMask)
            changesMask.SetAll();

        data.WriteBits(changesMask.GetBlock(0), 4);
        data.FlushBits();
        if (changesMask[0])
        {
            if (changesMask[1])
            {
                data.WriteUInt32(ProjectID);
            }
            if (changesMask[2])
            {
                data.WriteInt64(FirstCompleted);
            }
            if (changesMask[3])
            {
                data.WriteUInt32(CompletionCount);
            }
        }
    }

    public override void ClearChangesMask()
    {
        ClearChangesMask(ProjectID);
        ClearChangesMask(FirstCompleted);
        ClearChangesMask(CompletionCount);
        ChangesMask.ResetAll();
    }
}