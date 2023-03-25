﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class SpellEmpowerStageUpdate : ServerPacket
{
	public ObjectGuid CastID;
	public ObjectGuid Caster;
	public int TimeRemaining;
	public bool Unk;
	public List<uint> RemainingStageDurations = new();
	public SpellEmpowerStageUpdate() : base(ServerOpcodes.SpellEmpowerUpdate, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(CastID);
		_worldPacket.WritePackedGuid(Caster);
		_worldPacket.Write(TimeRemaining);
		_worldPacket.Write((uint)RemainingStageDurations.Count);
		_worldPacket.Write(Unk);

		foreach (var stageDuration in RemainingStageDurations)
			_worldPacket.Write(stageDuration);
	}
}