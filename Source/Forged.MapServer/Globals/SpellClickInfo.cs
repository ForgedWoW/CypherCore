// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Globals;

public class SpellClickInfo
{
    public byte CastFlags { get; set; }
    public uint SpellId { get; set; }
    public SpellClickUserTypes UserType { get; set; }

    // helpers
    public bool IsFitToRequirements(Unit clicker, Unit clickee)
    {
        var playerClicker = clicker.AsPlayer;

        if (playerClicker == null)
            return true;

        Unit summoner = null;

        // Check summoners for party
        if (clickee.IsSummon)
            summoner = clickee.ToTempSummon().GetSummonerUnit();

        summoner ??= clickee;

        // This only applies to players
        switch (UserType)
        {
            case SpellClickUserTypes.Friend:
                if (!playerClicker.WorldObjectCombat.IsFriendlyTo(summoner))
                    return false;

                break;
            case SpellClickUserTypes.Raid:
                if (!playerClicker.IsInRaidWith(summoner))
                    return false;

                break;
            case SpellClickUserTypes.Party:
                if (!playerClicker.IsInPartyWith(summoner))
                    return false;

                break;
        }

        return true;
    }
}