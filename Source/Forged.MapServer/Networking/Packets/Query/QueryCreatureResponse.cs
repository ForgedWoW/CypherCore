// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Query;

public class QueryCreatureResponse : ServerPacket
{
    public bool Allow;
    public uint CreatureID;
    public CreatureStats Stats;
    public QueryCreatureResponse() : base(ServerOpcodes.QueryCreatureResponse, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(CreatureID);
        WorldPacket.WriteBit(Allow);
        WorldPacket.FlushBits();

        if (Allow)
        {
            WorldPacket.WriteBits(Stats.Title.IsEmpty() ? 0 : Stats.Title.GetByteCount() + 1, 11);
            WorldPacket.WriteBits(Stats.TitleAlt.IsEmpty() ? 0 : Stats.TitleAlt.GetByteCount() + 1, 11);
            WorldPacket.WriteBits(Stats.CursorName.IsEmpty() ? 0 : Stats.CursorName.GetByteCount() + 1, 6);
            WorldPacket.WriteBit(Stats.Leader);

            for (var i = 0; i < SharedConst.MaxCreatureNames; ++i)
            {
                WorldPacket.WriteBits(Stats.Name[i].GetByteCount() + 1, 11);
                WorldPacket.WriteBits(Stats.NameAlt[i].GetByteCount() + 1, 11);
            }

            for (var i = 0; i < SharedConst.MaxCreatureNames; ++i)
            {
                if (!string.IsNullOrEmpty(Stats.Name[i]))
                    WorldPacket.WriteCString(Stats.Name[i]);

                if (!string.IsNullOrEmpty(Stats.NameAlt[i]))
                    WorldPacket.WriteCString(Stats.NameAlt[i]);
            }

            for (var i = 0; i < 2; ++i)
                WorldPacket.WriteUInt32(Stats.Flags[i]);

            WorldPacket.WriteInt32(Stats.CreatureType);
            WorldPacket.WriteInt32(Stats.CreatureFamily);
            WorldPacket.WriteInt32(Stats.Classification);

            for (var i = 0; i < SharedConst.MaxCreatureKillCredit; ++i)
                WorldPacket.WriteUInt32(Stats.ProxyCreatureID[i]);

            WorldPacket.WriteInt32(Stats.Display.CreatureDisplay.Count);
            WorldPacket.WriteFloat(Stats.Display.TotalProbability);

            foreach (var display in Stats.Display.CreatureDisplay)
            {
                WorldPacket.WriteUInt32(display.CreatureDisplayID);
                WorldPacket.WriteFloat(display.Scale);
                WorldPacket.WriteFloat(display.Probability);
            }

            WorldPacket.WriteFloat(Stats.HpMulti);
            WorldPacket.WriteFloat(Stats.EnergyMulti);

            WorldPacket.WriteInt32(Stats.QuestItems.Count);
            WorldPacket.WriteUInt32(Stats.CreatureMovementInfoID);
            WorldPacket.WriteInt32(Stats.HealthScalingExpansion);
            WorldPacket.WriteUInt32(Stats.RequiredExpansion);
            WorldPacket.WriteUInt32(Stats.VignetteID);
            WorldPacket.WriteInt32(Stats.Class);
            WorldPacket.WriteInt32(Stats.CreatureDifficultyID);
            WorldPacket.WriteInt32(Stats.WidgetSetID);
            WorldPacket.WriteInt32(Stats.WidgetSetUnitConditionID);

            if (!Stats.Title.IsEmpty())
                WorldPacket.WriteCString(Stats.Title);

            if (!Stats.TitleAlt.IsEmpty())
                WorldPacket.WriteCString(Stats.TitleAlt);

            if (!Stats.CursorName.IsEmpty())
                WorldPacket.WriteCString(Stats.CursorName);

            foreach (var questItem in Stats.QuestItems)
                WorldPacket.WriteUInt32(questItem);
        }
    }
}