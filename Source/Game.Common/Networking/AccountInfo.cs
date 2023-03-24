// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.Database;

namespace Game.Common.Networking;

public class AccountInfo
{
	public BattleNet battleNet;
	public Game game;

	public AccountInfo(SQLFields fields)
	{
		//         0             1           2          3                4            5           6          7            8      9     10          11
		// SELECT a.id, a.sessionkey, ba.last_ip, ba.locked, ba.lock_country, a.expansion, a.mutetime, ba.locale, a.recruiter, a.os, ba.id, aa.gmLevel,
		//                                                              12                                                            13    14
		// bab.unbandate > UNIX_TIMESTAMP() OR bab.unbandate = bab.bandate, ab.unbandate > UNIX_TIMESTAMP() OR ab.unbandate = ab.bandate, r.id
		// FROM account a LEFT JOIN battlenet_accounts ba ON a.battlenet_account = ba.id LEFT JOIN account_access aa ON a.id = aa.id AND aa.RealmID IN (-1, ?)
		// LEFT JOIN battlenet_account_bans bab ON ba.id = bab.id LEFT JOIN account_banned ab ON a.id = ab.id LEFT JOIN account r ON a.id = r.recruiter
		// WHERE a.username = ? ORDER BY aa.RealmID DESC LIMIT 1
		game.Id = fields.Read<uint>(0);
		game.SessionKey = fields.Read<byte[]>(1);
		battleNet.LastIP = fields.Read<string>(2);
		battleNet.IsLockedToIP = fields.Read<bool>(3);
		battleNet.LockCountry = fields.Read<string>(4);
		game.Expansion = fields.Read<byte>(5);
		game.MuteTime = fields.Read<long>(6);
		battleNet.Locale = (Locale)fields.Read<byte>(7);
		game.Recruiter = fields.Read<uint>(8);
		game.OS = fields.Read<string>(9);
		battleNet.Id = fields.Read<uint>(10);
		game.Security = (AccountTypes)fields.Read<byte>(11);
		battleNet.IsBanned = fields.Read<uint>(12) != 0;
		game.IsBanned = fields.Read<uint>(13) != 0;
		game.IsRectuiter = fields.Read<uint>(14) != 0;

		if (battleNet.Locale >= Locale.Total)
			battleNet.Locale = Locale.enUS;
	}

	public bool IsBanned()
	{
		return battleNet.IsBanned || game.IsBanned;
	}

	public struct BattleNet
	{
		public uint Id;
		public bool IsLockedToIP;
		public string LastIP;
		public string LockCountry;
		public Locale Locale;
		public bool IsBanned;
	}

	public struct Game
	{
		public uint Id;
		public byte[] SessionKey;
		public byte Expansion;
		public long MuteTime;
		public string OS;
		public uint Recruiter;
		public bool IsRectuiter;
		public AccountTypes Security;
		public bool IsBanned;
	}
}
