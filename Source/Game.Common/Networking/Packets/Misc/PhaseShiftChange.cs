// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class PhaseShiftChange : ServerPacket
{
	public ObjectGuid Client;
	public PhaseShiftData Phaseshift = new();
	public List<ushort> PreloadMapIDs = new();
	public List<ushort> UiMapPhaseIDs = new();
	public List<ushort> VisibleMapIDs = new();
	public PhaseShiftChange() : base(ServerOpcodes.PhaseShiftChange) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Client);
		Phaseshift.Write(_worldPacket);
		_worldPacket.WriteInt32(VisibleMapIDs.Count * 2); // size in bytes

		foreach (var visibleMapId in VisibleMapIDs)
			_worldPacket.WriteUInt16(visibleMapId); // Active terrain swap map id

		_worldPacket.WriteInt32(PreloadMapIDs.Count * 2); // size in bytes

		foreach (var preloadMapId in PreloadMapIDs)
			_worldPacket.WriteUInt16(preloadMapId); // Inactive terrain swap map id

		_worldPacket.WriteInt32(UiMapPhaseIDs.Count * 2); // size in bytes

		foreach (var uiMapPhaseId in UiMapPhaseIDs)
			_worldPacket.WriteUInt16(uiMapPhaseId); // UI map id, WorldMapArea.db2, controls map display
	}
}