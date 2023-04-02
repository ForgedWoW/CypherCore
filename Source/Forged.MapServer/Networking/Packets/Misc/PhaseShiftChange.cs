// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

internal class PhaseShiftChange : ServerPacket
{
    public ObjectGuid Client;
    public PhaseShiftData Phaseshift = new();
    public List<ushort> PreloadMapIDs = new();
    public List<ushort> UiMapPhaseIDs = new();
    public List<ushort> VisibleMapIDs = new();
    public PhaseShiftChange() : base(ServerOpcodes.PhaseShiftChange) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Client);
        Phaseshift.Write(WorldPacket);
        WorldPacket.WriteInt32(VisibleMapIDs.Count * 2); // size in bytes

        foreach (var visibleMapId in VisibleMapIDs)
            WorldPacket.WriteUInt16(visibleMapId); // Active terrain swap map id

        WorldPacket.WriteInt32(PreloadMapIDs.Count * 2); // size in bytes

        foreach (var preloadMapId in PreloadMapIDs)
            WorldPacket.WriteUInt16(preloadMapId); // Inactive terrain swap map id

        WorldPacket.WriteInt32(UiMapPhaseIDs.Count * 2); // size in bytes

        foreach (var uiMapPhaseId in UiMapPhaseIDs)
            WorldPacket.WriteUInt16(uiMapPhaseId); // UI map id, WorldMapArea.db2, controls map display
    }
}