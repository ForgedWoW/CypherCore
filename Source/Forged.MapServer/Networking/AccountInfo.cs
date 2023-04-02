// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.Networking;

internal class AccountInfo
{
    public BattleNet BNet;
    public Game GameInfo;

    public AccountInfo(SQLFields fields)
    {
        //         0             1           2          3                4            5           6          7            8      9     10          11
        // SELECT a.id, a.sessionkey, ba.last_ip, ba.locked, ba.lock_country, a.expansion, a.mutetime, ba.locale, a.recruiter, a.os, ba.id, aa.gmLevel,
        //                                                              12                                                            13    14
        // bab.unbandate > UNIX_TIMESTAMP() OR bab.unbandate = bab.bandate, ab.unbandate > UNIX_TIMESTAMP() OR ab.unbandate = ab.bandate, r.id
        // FROM account a LEFT JOIN battlenet_accounts ba ON a.battlenet_account = ba.id LEFT JOIN account_access aa ON a.id = aa.id AND aa.RealmID IN (-1, ?)
        // LEFT JOIN battlenet_account_bans bab ON ba.id = bab.id LEFT JOIN account_banned ab ON a.id = ab.id LEFT JOIN account r ON a.id = r.recruiter
        // WHERE a.username = ? ORDER BY aa.RealmID DESC LIMIT 1
        GameInfo.Id = fields.Read<uint>(0);
        GameInfo.SessionKey = fields.Read<byte[]>(1);
        BNet.LastIP = fields.Read<string>(2);
        BNet.IsLockedToIP = fields.Read<bool>(3);
        BNet.LockCountry = fields.Read<string>(4);
        GameInfo.Expansion = fields.Read<byte>(5);
        GameInfo.MuteTime = fields.Read<long>(6);
        BNet.Locale = (Locale)fields.Read<byte>(7);
        GameInfo.Recruiter = fields.Read<uint>(8);
        GameInfo.OS = fields.Read<string>(9);
        BNet.Id = fields.Read<uint>(10);
        GameInfo.Security = (AccountTypes)fields.Read<byte>(11);
        BNet.IsBanned = fields.Read<uint>(12) != 0;
        GameInfo.IsBanned = fields.Read<uint>(13) != 0;
        GameInfo.IsRectuiter = fields.Read<uint>(14) != 0;

        if (BNet.Locale >= Locale.Total)
            BNet.Locale = Locale.enUS;
    }

    public bool IsBanned()
    {
        return BNet.IsBanned || GameInfo.IsBanned;
    }

    public struct BattleNet
    {
        public uint Id;
        public bool IsBanned;
        public bool IsLockedToIP;
        public string LastIP;
        public Locale Locale;
        public string LockCountry;
    }

    public struct Game
    {
        public byte Expansion;
        public uint Id;
        public bool IsBanned;
        public bool IsRectuiter;
        public long MuteTime;
        public string OS;
        public uint Recruiter;
        public AccountTypes Security;
        public byte[] SessionKey;
    }
}