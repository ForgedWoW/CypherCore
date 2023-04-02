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
        _worldPacket.WriteUInt32(CreatureID);
        _worldPacket.WriteBit(Allow);
        _worldPacket.FlushBits();

        if (Allow)
        {
            _worldPacket.WriteBits(Stats.Title.IsEmpty() ? 0 : Stats.Title.GetByteCount() + 1, 11);
            _worldPacket.WriteBits(Stats.TitleAlt.IsEmpty() ? 0 : Stats.TitleAlt.GetByteCount() + 1, 11);
            _worldPacket.WriteBits(Stats.CursorName.IsEmpty() ? 0 : Stats.CursorName.GetByteCount() + 1, 6);
            _worldPacket.WriteBit(Stats.Leader);

            for (var i = 0; i < SharedConst.MaxCreatureNames; ++i)
            {
                _worldPacket.WriteBits(Stats.Name[i].GetByteCount() + 1, 11);
                _worldPacket.WriteBits(Stats.NameAlt[i].GetByteCount() + 1, 11);
            }

            for (var i = 0; i < SharedConst.MaxCreatureNames; ++i)
            {
                if (!string.IsNullOrEmpty(Stats.Name[i]))
                    _worldPacket.WriteCString(Stats.Name[i]);

                if (!string.IsNullOrEmpty(Stats.NameAlt[i]))
                    _worldPacket.WriteCString(Stats.NameAlt[i]);
            }

            for (var i = 0; i < 2; ++i)
                _worldPacket.WriteUInt32(Stats.Flags[i]);

            _worldPacket.WriteInt32(Stats.CreatureType);
            _worldPacket.WriteInt32(Stats.CreatureFamily);
            _worldPacket.WriteInt32(Stats.Classification);

            for (var i = 0; i < SharedConst.MaxCreatureKillCredit; ++i)
                _worldPacket.WriteUInt32(Stats.ProxyCreatureID[i]);

            _worldPacket.WriteInt32(Stats.Display.CreatureDisplay.Count);
            _worldPacket.WriteFloat(Stats.Display.TotalProbability);

            foreach (var display in Stats.Display.CreatureDisplay)
            {
                _worldPacket.WriteUInt32(display.CreatureDisplayID);
                _worldPacket.WriteFloat(display.Scale);
                _worldPacket.WriteFloat(display.Probability);
            }

            _worldPacket.WriteFloat(Stats.HpMulti);
            _worldPacket.WriteFloat(Stats.EnergyMulti);

            _worldPacket.WriteInt32(Stats.QuestItems.Count);
            _worldPacket.WriteUInt32(Stats.CreatureMovementInfoID);
            _worldPacket.WriteInt32(Stats.HealthScalingExpansion);
            _worldPacket.WriteUInt32(Stats.RequiredExpansion);
            _worldPacket.WriteUInt32(Stats.VignetteID);
            _worldPacket.WriteInt32(Stats.Class);
            _worldPacket.WriteInt32(Stats.CreatureDifficultyID);
            _worldPacket.WriteInt32(Stats.WidgetSetID);
            _worldPacket.WriteInt32(Stats.WidgetSetUnitConditionID);

            if (!Stats.Title.IsEmpty())
                _worldPacket.WriteCString(Stats.Title);

            if (!Stats.TitleAlt.IsEmpty())
                _worldPacket.WriteCString(Stats.TitleAlt);

            if (!Stats.CursorName.IsEmpty())
                _worldPacket.WriteCString(Stats.CursorName);

            foreach (var questItem in Stats.QuestItems)
                _worldPacket.WriteUInt32(questItem);
        }
    }
}