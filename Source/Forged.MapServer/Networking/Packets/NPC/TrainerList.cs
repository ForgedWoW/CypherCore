// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.NPC;

public class TrainerList : ServerPacket
{
    public ObjectGuid TrainerGUID;
    public int TrainerType;
    public int TrainerID = 1;
    public List<TrainerListSpell> Spells = new();
    public string Greeting;
    public TrainerList() : base(ServerOpcodes.TrainerList, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(TrainerGUID);
        _worldPacket.WriteInt32(TrainerType);
        _worldPacket.WriteInt32(TrainerID);

        _worldPacket.WriteInt32(Spells.Count);

        foreach (var spell in Spells)
        {
            _worldPacket.WriteUInt32(spell.SpellID);
            _worldPacket.WriteUInt32(spell.MoneyCost);
            _worldPacket.WriteUInt32(spell.ReqSkillLine);
            _worldPacket.WriteUInt32(spell.ReqSkillRank);

            for (uint i = 0; i < SharedConst.MaxTrainerspellAbilityReqs; ++i)
                _worldPacket.WriteUInt32(spell.ReqAbility[i]);

            _worldPacket.WriteUInt8((byte)spell.Usable);
            _worldPacket.WriteUInt8(spell.ReqLevel);
        }

        _worldPacket.WriteBits(Greeting.GetByteCount(), 11);
        _worldPacket.FlushBits();
        _worldPacket.WriteString(Greeting);
    }
}