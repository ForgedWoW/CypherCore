// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Pet;

internal class PetRename : ClientPacket
{
    public PetRenameData RenameData;
    public PetRename(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        RenameData.PetGUID = WorldPacket.ReadPackedGuid();
        RenameData.PetNumber = WorldPacket.ReadInt32();

        var nameLen = WorldPacket.ReadBits<uint>(8);

        RenameData.HasDeclinedNames = WorldPacket.HasBit();

        if (RenameData.HasDeclinedNames)
        {
            RenameData.DeclinedNames = new DeclinedName();
            var count = new uint[SharedConst.MaxDeclinedNameCases];

            for (var i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
                count[i] = WorldPacket.ReadBits<uint>(7);

            for (var i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
                RenameData.DeclinedNames.Name[i] = WorldPacket.ReadString(count[i]);
        }

        RenameData.NewName = WorldPacket.ReadString(nameLen);
    }
}