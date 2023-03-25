// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

class DisplayGameError : ServerPacket
{
	readonly GameError Error;
	readonly int? Arg;
	readonly int? Arg2;

	public DisplayGameError(GameError error) : base(ServerOpcodes.DisplayGameError)
	{
		Error = error;
	}

	public DisplayGameError(GameError error, int arg) : this(error)
	{
		Arg = arg;
	}

	public DisplayGameError(GameError error, int arg1, int arg2) : this(error)
	{
		Arg = arg1;
		Arg2 = arg2;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32((uint)Error);
		_worldPacket.WriteBit(Arg.HasValue);
		_worldPacket.WriteBit(Arg2.HasValue);
		_worldPacket.FlushBits();

		if (Arg.HasValue)
			_worldPacket.WriteInt32(Arg.Value);

		if (Arg2.HasValue)
			_worldPacket.WriteInt32(Arg2.Value);
	}
}