// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Warden;
using Framework.IO;

namespace Forged.MapServer.Warden;

internal class WardenInitModuleRequest
{
    public uint CheckSumm1 { get; set; }
    public uint CheckSumm2 { get; set; }
    public uint CheckSumm3 { get; set; }
    public WardenOpcodes Command1 { get; set; }
    public WardenOpcodes Command2 { get; set; }
    public WardenOpcodes Command3 { get; set; }
    public uint[] Function1 { get; set; } = new uint[4];
    public uint Function2 { get; set; }
    public byte Function2Set { get; set; }
    public uint Function3 { get; set; }
    public byte Function3Set { get; set; }
    public ushort Size1 { get; set; }
    public ushort Size2 { get; set; }
    public ushort Size3 { get; set; }
    public byte StringLibrary1 { get; set; }
    public byte StringLibrary2 { get; set; }
    public byte StringLibrary3 { get; set; }
    public byte Type { get; set; }
    public byte Unk1 { get; set; }
    public byte Unk2 { get; set; }
    public byte Unk3 { get; set; }
    public byte Unk4 { get; set; }
    public byte Unk5 { get; set; }
    public byte Unk6 { get; set; }

    public static implicit operator byte[](WardenInitModuleRequest request)
    {
        var buffer = new ByteBuffer();
        buffer.WriteUInt8((byte)request.Command1);
        buffer.WriteUInt16(request.Size1);
        buffer.WriteUInt32(request.CheckSumm1);
        buffer.WriteUInt8(request.Unk1);
        buffer.WriteUInt8(request.Unk2);
        buffer.WriteUInt8(request.Type);
        buffer.WriteUInt8(request.StringLibrary1);

        foreach (var function in request.Function1)
            buffer.WriteUInt32(function);

        buffer.WriteUInt8((byte)request.Command2);
        buffer.WriteUInt16(request.Size2);
        buffer.WriteUInt32(request.CheckSumm2);
        buffer.WriteUInt8(request.Unk3);
        buffer.WriteUInt8(request.Unk4);
        buffer.WriteUInt8(request.StringLibrary2);
        buffer.WriteUInt32(request.Function2);
        buffer.WriteUInt8(request.Function2Set);

        buffer.WriteUInt8((byte)request.Command3);
        buffer.WriteUInt16(request.Size3);
        buffer.WriteUInt32(request.CheckSumm3);
        buffer.WriteUInt8(request.Unk5);
        buffer.WriteUInt8(request.Unk6);
        buffer.WriteUInt8(request.StringLibrary3);
        buffer.WriteUInt32(request.Function3);
        buffer.WriteUInt8(request.Function3Set);

        return buffer.GetData();
    }
}