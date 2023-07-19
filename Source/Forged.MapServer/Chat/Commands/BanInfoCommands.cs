// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Cache;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Framework.Constants;
using Framework.Database;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("baninfo")]
internal class BanInfoCommands
{
    [Command("account", RBACPermissions.CommandBaninfoAccount, true)]
    private static bool HandleBanInfoAccountCommand(CommandHandler handler, string accountName)
    {
        if (accountName.IsEmpty())
            return false;

        var accountId = handler.AccountManager.GetId(accountName);

        if (accountId == 0)
        {
            handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);

            return true;
        }

        return HandleBanInfoHelper(accountId, accountName, handler);
    }

    [Command("character", RBACPermissions.CommandBaninfoCharacter, true)]
    private static bool HandleBanInfoCharacterCommand(CommandHandler handler, string name)
    {
        if (!handler.ObjectManager.NormalizePlayerName(ref name))
        {
            handler.SendSysMessage(CypherStrings.BaninfoNocharacter);

            return false;
        }

        var target = handler.ClassFactory.Resolve<ObjectAccessor>().FindPlayerByName(name);
        ObjectGuid targetGuid;

        if (target == null)
        {
            targetGuid = handler.ClassFactory.Resolve<CharacterCache>().GetCharacterGuidByName(name);

            if (targetGuid.IsEmpty)
            {
                handler.SendSysMessage(CypherStrings.BaninfoNocharacter);

                return false;
            }
        }
        else
            targetGuid = target.GUID;

        var charDb = handler.ClassFactory.Resolve<CharacterDatabase>();
        var stmt = charDb.GetPreparedStatement(CharStatements.SEL_BANINFO);
        stmt.AddValue(0, targetGuid.Counter);
        var result = charDb.Query(stmt);

        if (result.IsEmpty())
        {
            handler.SendSysMessage(CypherStrings.CharNotBanned, name);

            return true;
        }

        handler.SendSysMessage(CypherStrings.BaninfoBanhistory, name);

        do
        {
            var unbanDate = result.Read<long>(3);
            var active = result.Read<bool>(2) && (result.Read<long>(1) == 0L || unbanDate >= GameTime.CurrentTime);

            var permanent = result.Read<long>(1) == 0L;
            var banTime = permanent ? handler.GetCypherString(CypherStrings.BaninfoInfinite) : Time.SecsToTimeString(result.Read<ulong>(1), TimeFormat.ShortText);

            handler.SendSysMessage(CypherStrings.BaninfoHistoryentry,
                                   Time.UnixTimeToDateTime(result.Read<long>(0)).ToShortTimeString(),
                                   banTime,
                                   active ? handler.GetCypherString(CypherStrings.Yes) : handler.GetCypherString(CypherStrings.No),
                                   result.Read<string>(4),
                                   result.Read<string>(5));
        } while (result.NextRow());

        return true;
    }

    private static bool HandleBanInfoHelper(uint accountId, string accountName, CommandHandler handler)
    {
        var result = handler.ClassFactory.Resolve<LoginDatabase>().Query("SELECT FROM_UNIXTIME(bandate), unbandate-bandate, active, unbandate, banreason, bannedby FROM account_banned WHERE id = '{0}' ORDER BY bandate ASC", accountId);

        if (result.IsEmpty())
        {
            handler.SendSysMessage(CypherStrings.BaninfoNoaccountban, accountName);

            return true;
        }

        handler.SendSysMessage(CypherStrings.BaninfoBanhistory, accountName);

        do
        {
            long unbanDate = result.Read<uint>(3);
            var active = result.Read<bool>(2) && (result.Read<ulong>(1) == 0 || unbanDate >= GameTime.CurrentTime);

            var permanent = result.Read<ulong>(1) == 0;
            var banTime = permanent ? handler.GetCypherString(CypherStrings.BaninfoInfinite) : Time.SecsToTimeString(result.Read<ulong>(1), TimeFormat.ShortText);

            handler.SendSysMessage(CypherStrings.BaninfoHistoryentry,
                                   result.Read<string>(0),
                                   banTime,
                                   active ? handler.GetCypherString(CypherStrings.Yes) : handler.GetCypherString(CypherStrings.No),
                                   result.Read<string>(4),
                                   result.Read<string>(5));
        } while (result.NextRow());

        return true;
    }

    [Command("ip", RBACPermissions.CommandBaninfoIp, true)]
    private static bool HandleBanInfoIPCommand(CommandHandler handler, string ip)
    {
        if (ip.IsEmpty())
            return false;

        var result = handler.ClassFactory.Resolve<LoginDatabase>().Query("SELECT ip, FROM_UNIXTIME(bandate), FROM_UNIXTIME(unbandate), unbandate-UNIX_TIMESTAMP(), banreason, bannedby, unbandate-bandate FROM ip_banned WHERE ip = '{0}'", ip);

        if (result.IsEmpty())
        {
            handler.SendSysMessage(CypherStrings.BaninfoNoip);

            return true;
        }

        var permanent = result.Read<ulong>(6) == 0;

        handler.SendSysMessage(CypherStrings.BaninfoIpentry,
                               result.Read<string>(0),
                               result.Read<string>(1),
                               permanent ? handler.GetCypherString(CypherStrings.BaninfoNever) : result.Read<string>(2),
                               permanent ? handler.GetCypherString(CypherStrings.BaninfoInfinite) : Time.SecsToTimeString(result.Read<ulong>(3), TimeFormat.ShortText),
                               result.Read<string>(4),
                               result.Read<string>(5));

        return true;
    }
}