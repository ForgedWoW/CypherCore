// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattlePet;

internal class PetBattleSlotUpdates : ServerPacket
{
    public bool AutoSlotted;
    public bool NewSlot;
    public List<BattlePetSlot> Slots = new();
    public PetBattleSlotUpdates() : base(ServerOpcodes.PetBattleSlotUpdates) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(Slots.Count);
        WorldPacket.WriteBit(NewSlot);
        WorldPacket.WriteBit(AutoSlotted);
        WorldPacket.FlushBits();

        foreach (var slot in Slots)
            slot.Write(WorldPacket);
    }
}