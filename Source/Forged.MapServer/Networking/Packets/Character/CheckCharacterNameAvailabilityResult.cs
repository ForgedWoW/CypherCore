// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Character;

internal class CheckCharacterNameAvailabilityResult : ServerPacket
{
    public ResponseCodes Result;
    public uint SequenceIndex;
    public CheckCharacterNameAvailabilityResult(uint sequenceIndex, ResponseCodes result) : base(ServerOpcodes.CheckCharacterNameAvailabilityResult)
    {
        SequenceIndex = sequenceIndex;
        Result = result;
    }

    public override void Write()
    {
        _worldPacket.WriteUInt32(SequenceIndex);
        _worldPacket.WriteUInt32((uint)Result);
    }
}