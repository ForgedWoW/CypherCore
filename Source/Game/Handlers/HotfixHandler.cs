﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.DataStorage;
using Game.Networking;
using Game.Networking.Packets;

namespace Game;

public partial class WorldSession
{
	[WorldPacketHandler(ClientOpcodes.DbQueryBulk, Processing = PacketProcessing.Inplace, Status = SessionStatus.Authed)]
	void HandleDBQueryBulk(DBQueryBulk dbQuery)
	{
		var store = Global.DB2Mgr.GetStorage(dbQuery.TableHash);

		foreach (var record in dbQuery.Queries)
		{
			DBReply dbReply = new();
			dbReply.TableHash = dbQuery.TableHash;
			dbReply.RecordID = record.RecordID;

			if (store != null && store.HasRecord(record.RecordID))
			{
				dbReply.Status = HotfixRecord.Status.Valid;
				dbReply.Timestamp = (uint)GameTime.GetGameTime();
				store.WriteRecord(record.RecordID, SessionDbcLocale, dbReply.Data);

				var optionalDataEntries = Global.DB2Mgr.GetHotfixOptionalData(dbQuery.TableHash, record.RecordID, SessionDbcLocale);

				foreach (var optionalData in optionalDataEntries)
				{
					dbReply.Data.WriteUInt32(optionalData.Key);
					dbReply.Data.WriteBytes(optionalData.Data);
				}
			}
			else
			{
				Log.outTrace(LogFilter.Network, "CMSG_DB_QUERY_BULK: {0} requested non-existing entry {1} in datastore: {2}", GetPlayerInfo(), record.RecordID, dbQuery.TableHash);
				dbReply.Timestamp = (uint)GameTime.GetGameTime();
			}

			SendPacket(dbReply);
		}
	}

	void SendAvailableHotfixes()
	{
		SendPacket(new AvailableHotfixes(Global.WorldMgr.RealmId.GetAddress(), Global.DB2Mgr.GetHotfixData()));
	}

	[WorldPacketHandler(ClientOpcodes.HotfixRequest, Status = SessionStatus.Authed)]
	void HandleHotfixRequest(HotfixRequest hotfixQuery)
	{
		var hotfixes = Global.DB2Mgr.GetHotfixData();

		HotfixConnect hotfixQueryResponse = new();

		foreach (var hotfixId in hotfixQuery.Hotfixes)
		{
			var hotfixRecords = hotfixes.LookupByKey(hotfixId);

			if (hotfixRecords != null)
				foreach (var hotfixRecord in hotfixRecords)
				{
					HotfixConnect.HotfixData hotfixData = new();
					hotfixData.Record = hotfixRecord;

					if (hotfixRecord.HotfixStatus == HotfixRecord.Status.Valid)
					{
						var storage = Global.DB2Mgr.GetStorage(hotfixRecord.TableHash);

						if (storage != null && storage.HasRecord((uint)hotfixRecord.RecordID))
						{
							var pos = hotfixQueryResponse.HotfixContent.GetSize();
							storage.WriteRecord((uint)hotfixRecord.RecordID, SessionDbcLocale, hotfixQueryResponse.HotfixContent);

							var optionalDataEntries = Global.DB2Mgr.GetHotfixOptionalData(hotfixRecord.TableHash, (uint)hotfixRecord.RecordID, SessionDbcLocale);

							if (optionalDataEntries != null)
								foreach (var optionalData in optionalDataEntries)
								{
									hotfixQueryResponse.HotfixContent.WriteUInt32(optionalData.Key);
									hotfixQueryResponse.HotfixContent.WriteBytes(optionalData.Data);
								}

							hotfixData.Size = hotfixQueryResponse.HotfixContent.GetSize() - pos;
						}
						else
						{
							var blobData = Global.DB2Mgr.GetHotfixBlobData(hotfixRecord.TableHash, hotfixRecord.RecordID, SessionDbcLocale);

							if (blobData != null)
							{
								hotfixData.Size = (uint)blobData.Length;
								hotfixQueryResponse.HotfixContent.WriteBytes(blobData);
							}
							else
								// Do not send Status::Valid when we don't have a hotfix blob for current locale
							{
								hotfixData.Record.HotfixStatus = storage != null ? HotfixRecord.Status.RecordRemoved : HotfixRecord.Status.Invalid;
							}
						}
					}

					hotfixQueryResponse.Hotfixes.Add(hotfixData);
				}
		}

		SendPacket(hotfixQueryResponse);
	}
}