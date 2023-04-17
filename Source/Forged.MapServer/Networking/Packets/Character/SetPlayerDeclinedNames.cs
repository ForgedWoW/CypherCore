// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Character;

internal class SetPlayerDeclinedNames : ClientPacket
{
    public DeclinedName DeclinedNames;
    public ObjectGuid Player;

    public SetPlayerDeclinedNames(WorldPacket packet) : base(packet)
    {
        DeclinedNames = new DeclinedName();
    }

    public override void Read()
    {
        Player = WorldPacket.ReadPackedGuid();

        var stringLengths = new byte[SharedConst.MaxDeclinedNameCases];

        for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
            stringLengths[i] = WorldPacket.ReadBits<byte>(7);

        for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
            DeclinedNames.Name[i] = WorldPacket.ReadString(stringLengths[i]);
    }
}