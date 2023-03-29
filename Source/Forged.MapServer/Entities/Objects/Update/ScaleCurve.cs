// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;

namespace Forged.MapServer.Entities.Objects.Update;

public class ScaleCurve : BaseUpdateData<AreaTrigger>
{
    public UpdateField<bool> OverrideActive = new(0, 1);
    public UpdateField<uint> StartTimeOffset = new(0, 2);
    public UpdateField<uint> ParameterCurve = new(0, 3);
    public UpdateFieldArray<Vector2> Points = new(2, 4, 5);

    public ScaleCurve() : base(7) { }

    public void WriteCreate(WorldPacket data, AreaTrigger owner, Player receiver)
    {
        data.WriteUInt32(StartTimeOffset);

        for (var i = 0; i < 2; ++i)
            data.WriteVector2(Points[i]);

        data.WriteUInt32(ParameterCurve);
        data.WriteBit((bool)OverrideActive);
        data.FlushBits();
    }

    public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, AreaTrigger owner, Player receiver)
    {
        var changesMask = ChangesMask;

        if (ignoreChangesMask)
            changesMask.SetAll();

        data.WriteBits(changesMask.GetBlock(0), 7);

        if (changesMask[0])
            if (changesMask[1])
                data.WriteBit(OverrideActive);

        data.FlushBits();

        if (changesMask[0])
        {
            if (changesMask[2])
                data.WriteUInt32(StartTimeOffset);

            if (changesMask[3])
                data.WriteUInt32(ParameterCurve);
        }

        if (changesMask[4])
            for (var i = 0; i < 2; ++i)
                if (changesMask[5 + i])
                    data.WriteVector2(Points[i]);

        data.FlushBits();
    }

    public override void ClearChangesMask()
    {
        ClearChangesMask(OverrideActive);
        ClearChangesMask(StartTimeOffset);
        ClearChangesMask(ParameterCurve);
        ClearChangesMask(Points);
        ChangesMask.ResetAll();
    }
}