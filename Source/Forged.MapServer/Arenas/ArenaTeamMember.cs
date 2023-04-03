// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Framework.Constants;

namespace Forged.MapServer.Arenas;

public class ArenaTeamMember
{
    public byte Class;
    public ObjectGuid Guid;
    public ushort MatchMakerRating;
    public string Name;
    public ushort PersonalRating;
    public ushort SeasonGames;
    public ushort SeasonWins;
    public ushort WeekGames;
    public ushort WeekWins;

    public void ModifyMatchmakerRating(int mod, uint slot)
    {
        if (MatchMakerRating + mod < 0)
            MatchMakerRating = 0;
        else
            MatchMakerRating += (ushort)mod;
    }

    public void ModifyPersonalRating(Player player, int mod, uint type)
    {
        if (PersonalRating + mod < 0)
            PersonalRating = 0;
        else
            PersonalRating += (ushort)mod;

        if (player)
        {
            player.SetArenaTeamInfoField(ArenaTeam.GetSlotByType(type), ArenaTeamInfoType.PersonalRating, PersonalRating);
            player.UpdateCriteria(CriteriaType.EarnPersonalArenaRating, PersonalRating, type);
        }
    }
}