// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Warden;
using Framework.IO;

namespace Forged.MapServer.Warden;

internal class WardenInitModuleRequest
{
	public WardenOpcodes Command1;
	public ushort Size1;
	public uint CheckSumm1;
	public byte Unk1;
	public byte Unk2;
	public byte Type;
	public byte String_library1;
	public uint[] Function1 = new uint[4];

	public WardenOpcodes Command2;
	public ushort Size2;
	public uint CheckSumm2;
	public byte Unk3;
	public byte Unk4;
	public byte String_library2;
	public uint Function2;
	public byte Function2_set;

	public WardenOpcodes Command3;
	public ushort Size3;
	public uint CheckSumm3;
	public byte Unk5;
	public byte Unk6;
	public byte String_library3;
	public uint Function3;
	public byte Function3_set;

	public static implicit operator byte[](WardenInitModuleRequest request)
	{
		var buffer = new ByteBuffer();
		buffer.WriteUInt8((byte)request.Command1);
		buffer.WriteUInt16(request.Size1);
		buffer.WriteUInt32(request.CheckSumm1);
		buffer.WriteUInt8(request.Unk1);
		buffer.WriteUInt8(request.Unk2);
		buffer.WriteUInt8(request.Type);
		buffer.WriteUInt8(request.String_library1);

		foreach (var function in request.Function1)
			buffer.WriteUInt32(function);

		buffer.WriteUInt8((byte)request.Command2);
		buffer.WriteUInt16(request.Size2);
		buffer.WriteUInt32(request.CheckSumm2);
		buffer.WriteUInt8(request.Unk3);
		buffer.WriteUInt8(request.Unk4);
		buffer.WriteUInt8(request.String_library2);
		buffer.WriteUInt32(request.Function2);
		buffer.WriteUInt8(request.Function2_set);

		buffer.WriteUInt8((byte)request.Command3);
		buffer.WriteUInt16(request.Size3);
		buffer.WriteUInt32(request.CheckSumm3);
		buffer.WriteUInt8(request.Unk5);
		buffer.WriteUInt8(request.Unk6);
		buffer.WriteUInt8(request.String_library3);
		buffer.WriteUInt32(request.Function3);
		buffer.WriteUInt8(request.Function3_set);

		return buffer.GetData();
	}
}