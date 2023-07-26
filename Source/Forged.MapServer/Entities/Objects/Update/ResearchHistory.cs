// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;
using System;

namespace Forged.MapServer.Entities.Objects.Update;

public class ResearchHistory : BaseUpdateData<ResearchHistory>
{
    public DynamicUpdateField<CompletedProject> CompletedProjects = new(0, 1);

    public ResearchHistory()  : base(2)
    {

    }

    public void WriteCreate(WorldPacket data, Player owner, Player receiver)
    {
        data.WriteUInt32((uint)CompletedProjects.Size());

        foreach (var project in CompletedProjects)
        {
            project.WriteCreate(data, owner, receiver);
        }
    }

    public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
    {
        var changesMask = ChangesMask;

        if (ignoreChangesMask)
            changesMask.SetAll();

        data.WriteBits(changesMask.GetBlock(0), 2);
        
        if (changesMask[0])
        {
            if (changesMask[1])
            {
                if (!ignoreChangesMask)
                    CompletedProjects.WriteUpdateMask(data);
                else
                    WriteCompleteDynamicFieldUpdateMask(CompletedProjects.Size(), data);
            }
        }

        data.FlushBits();

        if (changesMask[0])
        {
            if (changesMask[1])
            {
                foreach (var project in CompletedProjects)
                {
                    if (CompletedProjects.HasChanged(CompletedProjects.FindIndex(project)) 
                        || ignoreChangesMask)
                    {
                        project.WriteUpdate(data, ignoreChangesMask, owner, receiver);
                    }
                }
            }
        }

    }

    public override void ClearChangesMask()
    {
        ClearChangesMask(CompletedProjects);
        ChangesMask.ResetAll();
    }
}