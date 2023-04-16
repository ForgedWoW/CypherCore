// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Maps.Workers;
using Forged.MapServer.Networking.Packets.Chat;
using Forged.MapServer.Text;
using Framework.Constants;

namespace Forged.MapServer.OutdoorPVP;

internal class DefenseMessageBuilder : MessageBuilder
{
    private readonly uint _id;
    private readonly OutdoorPvPManager _outdoorPvPManager;

    private readonly uint _zoneId; // ZoneId
    // BroadcastTextId

    public DefenseMessageBuilder(uint zoneId, uint id, OutdoorPvPManager outdoorPvPManager)
    {
        _zoneId = zoneId;
        _id = id;
        _outdoorPvPManager = outdoorPvPManager;
    }

    public override PacketSenderOwning<DefenseMessage> Invoke(Locale locale = Locale.enUS)
    {
        var text = _outdoorPvPManager.GetDefenseMessage(_zoneId, _id, locale);

        PacketSenderOwning<DefenseMessage> defenseMessage = new()
        {
            Data =
            {
                ZoneID = _zoneId,
                MessageText = text
            }
        };

        return defenseMessage;
    }
}