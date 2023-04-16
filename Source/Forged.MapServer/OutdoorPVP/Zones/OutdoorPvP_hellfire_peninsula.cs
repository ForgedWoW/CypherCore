// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Maps;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IOutdoorPvP;

namespace Forged.MapServer.OutdoorPVP.Zones;

[Script]
internal class OutdoorPvPHellfirePeninsula : ScriptObjectAutoAddDBBound, IOutdoorPvPGetOutdoorPvP
{
    public OutdoorPvPHellfirePeninsula() : base("outdoorpvp_hp") { }

    public OutdoorPvP GetOutdoorPvP(Map map)
    {
        return new HellfirePeninsulaPvP(map);
    }
}