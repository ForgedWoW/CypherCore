// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattlePet;

internal class BattlePetModifyName : ClientPacket
{
    public DeclinedName DeclinedNames;
    public string Name;
    public ObjectGuid PetGuid;
    public BattlePetModifyName(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PetGuid = WorldPacket.ReadPackedGuid();
        var nameLength = WorldPacket.ReadBits<uint>(7);

        if (WorldPacket.HasBit())
        {
            DeclinedNames = new DeclinedName();

            var declinedNameLengths = new byte[SharedConst.MaxDeclinedNameCases];

            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                declinedNameLengths[i] = WorldPacket.ReadBits<byte>(7);

            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                DeclinedNames.Name[i] = WorldPacket.ReadString(declinedNameLengths[i]);
        }

        Name = WorldPacket.ReadString(nameLength);
    }
}