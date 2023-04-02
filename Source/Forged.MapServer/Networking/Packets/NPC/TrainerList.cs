// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.NPC;

public class TrainerList : ServerPacket
{
    public string Greeting;
    public List<TrainerListSpell> Spells = new();
    public ObjectGuid TrainerGUID;
    public int TrainerID = 1;
    public int TrainerType;
    public TrainerList() : base(ServerOpcodes.TrainerList, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(TrainerGUID);
        WorldPacket.WriteInt32(TrainerType);
        WorldPacket.WriteInt32(TrainerID);

        WorldPacket.WriteInt32(Spells.Count);

        foreach (var spell in Spells)
        {
            WorldPacket.WriteUInt32(spell.SpellID);
            WorldPacket.WriteUInt32(spell.MoneyCost);
            WorldPacket.WriteUInt32(spell.ReqSkillLine);
            WorldPacket.WriteUInt32(spell.ReqSkillRank);

            for (uint i = 0; i < SharedConst.MaxTrainerspellAbilityReqs; ++i)
                WorldPacket.WriteUInt32(spell.ReqAbility[i]);

            WorldPacket.WriteUInt8((byte)spell.Usable);
            WorldPacket.WriteUInt8(spell.ReqLevel);
        }

        WorldPacket.WriteBits(Greeting.GetByteCount(), 11);
        WorldPacket.FlushBits();
        WorldPacket.WriteString(Greeting);
    }
}