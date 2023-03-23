﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.GameObject;

public class GameObjectPlaySpellVisual : ServerPacket
{
	public ObjectGuid ObjectGUID;
	public ObjectGuid ActivatorGUID;
	public uint SpellVisualID;
	public GameObjectPlaySpellVisual() : base(ServerOpcodes.GameObjectPlaySpellVisual) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ObjectGUID);
		_worldPacket.WritePackedGuid(ActivatorGUID);
		_worldPacket.WriteUInt32(SpellVisualID);
	}
}
