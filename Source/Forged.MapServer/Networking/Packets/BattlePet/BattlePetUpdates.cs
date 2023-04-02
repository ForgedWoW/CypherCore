﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattlePet;

internal class BattlePetUpdates : ServerPacket
{
    public bool PetAdded;
    public List<BattlePetStruct> Pets = new();
    public BattlePetUpdates() : base(ServerOpcodes.BattlePetUpdates) { }

    public override void Write()
    {
        _worldPacket.WriteInt32(Pets.Count);
        _worldPacket.WriteBit(PetAdded);
        _worldPacket.FlushBits();

        foreach (var pet in Pets)
            pet.Write(_worldPacket);
    }
}