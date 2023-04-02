// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Pet;

internal class SetPetSpecialization : ServerPacket
{
    public ushort SpecID;
    public SetPetSpecialization() : base(ServerOpcodes.SetPetSpecialization) { }

    public override void Write()
    {
        WorldPacket.WriteUInt16(SpecID);
    }
}