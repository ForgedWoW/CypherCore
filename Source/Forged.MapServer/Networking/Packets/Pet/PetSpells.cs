// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Pet;

public class PetSpells : ServerPacket
{
    public uint[] ActionButtons = new uint[10];
    public List<uint> Actions = new();
    public CommandStates CommandState;
    public List<PetSpellCooldown> Cooldowns = new();
    public ushort CreatureFamily;
    public byte Flag;
    public ObjectGuid PetGUID;
    public ReactStates ReactState;
    public ushort Specialization;
    public List<PetSpellHistory> SpellHistory = new();
    public uint TimeLimit;
    public PetSpells() : base(ServerOpcodes.PetSpellsMessage, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(PetGUID);
        WorldPacket.WriteUInt16(CreatureFamily);
        WorldPacket.WriteUInt16(Specialization);
        WorldPacket.WriteUInt32(TimeLimit);
        WorldPacket.WriteUInt16((ushort)((byte)CommandState | (Flag << 16)));
        WorldPacket.WriteUInt8((byte)ReactState);

        foreach (var actionButton in ActionButtons)
            WorldPacket.WriteUInt32(actionButton);

        WorldPacket.WriteInt32(Actions.Count);
        WorldPacket.WriteInt32(Cooldowns.Count);
        WorldPacket.WriteInt32(SpellHistory.Count);

        foreach (var action in Actions)
            WorldPacket.WriteUInt32(action);

        foreach (var cooldown in Cooldowns)
        {
            WorldPacket.WriteUInt32(cooldown.SpellID);
            WorldPacket.WriteUInt32(cooldown.Duration);
            WorldPacket.WriteUInt32(cooldown.CategoryDuration);
            WorldPacket.WriteFloat(cooldown.ModRate);
            WorldPacket.WriteUInt16(cooldown.Category);
        }

        foreach (var history in SpellHistory)
        {
            WorldPacket.WriteUInt32(history.CategoryID);
            WorldPacket.WriteUInt32(history.RecoveryTime);
            WorldPacket.WriteFloat(history.ChargeModRate);
            WorldPacket.WriteInt8(history.ConsumedCharges);
        }
    }
}