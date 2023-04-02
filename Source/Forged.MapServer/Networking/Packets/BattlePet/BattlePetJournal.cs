// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattlePet;

internal class BattlePetJournal : ServerPacket
{
    public bool HasJournalLock = false;
    public List<BattlePetStruct> Pets = new();
    public List<BattlePetSlot> Slots = new();
    public ushort Trap;
    public BattlePetJournal() : base(ServerOpcodes.BattlePetJournal) { }

    public override void Write()
    {
        WorldPacket.WriteUInt16(Trap);
        WorldPacket.WriteInt32(Slots.Count);
        WorldPacket.WriteInt32(Pets.Count);
        WorldPacket.WriteBit(HasJournalLock);
        WorldPacket.FlushBits();

        foreach (var slot in Slots)
            slot.Write(WorldPacket);

        foreach (var pet in Pets)
            pet.Write(WorldPacket);
    }
}

//Structs