// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.DataStorage.ClientReader;
using Forged.RealmServer.Networking;
using Game.Common.Handlers;
using Forged.RealmServer.Networking.Packets;
using Serilog;

namespace Forged.RealmServer.Handlers;

public class HotfixHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly Dictionary<uint, IDB2Storage> _storage;
    private readonly MultiMap<int, HotfixRecord> _hotfixData;
    private readonly MultiMap<(uint tableHash, int recordId), HotfixOptionalData>[] _optionalData;
    private readonly Dictionary<(uint tableHash, int recordId), byte[]>[] _hotfixBlobData;
    private readonly GameTime _gameTime;
    private readonly DB2Manager _dB2Manager;

    public HotfixHandler(WorldSession session, Dictionary<uint, IDB2Storage> storage, MultiMap<int, HotfixRecord> hotfixData, MultiMap<(uint tableHash, int recordId), 
		HotfixOptionalData>[] optionalData, Dictionary<(uint tableHash, int recordId), byte[]>[] hotfixBlobData, GameTime gameTime, DB2Manager dB2Manager)
    {
        _session = session;
        _storage = storage;
        _hotfixData = hotfixData;
        _optionalData = optionalData;
        _hotfixBlobData = hotfixBlobData;
        _gameTime = gameTime;
        _dB2Manager = dB2Manager;
    }

	[WorldPacketHandler(ClientOpcodes.DbQueryBulk, Processing = PacketProcessing.Inplace, Status = SessionStatus.Authed)]
	void HandleDBQueryBulk(DBQueryBulk dbQuery)
	{
		var store = _storage.LookupByKey(dbQuery.TableHash);

		foreach (var record in dbQuery.Queries)
		{
			DBReply dbReply = new();
			dbReply.TableHash = dbQuery.TableHash;
			dbReply.RecordID = record.RecordID;

			if (store != null && store.HasRecord(record.RecordID))
            {
                dbReply.Status = HotfixRecord.Status.Valid;
				dbReply.Timestamp = (uint)_gameTime.GetGameTime;
				store.WriteRecord(record.RecordID, _session.SessionDbcLocale, dbReply.Data);

				if (_optionalData[(int)_session.SessionDbcLocale].TryGetValue((dbQuery.TableHash, (int)record.RecordID), out var optionalDataEntries))
				    foreach (var optionalData in optionalDataEntries)
				    {
					    dbReply.Data.WriteUInt32(optionalData.Key);
					    dbReply.Data.WriteBytes(optionalData.Data);
				    }
			}
			else
			{
				Log.Logger.Verbose("CMSG_DB_QUERY_BULK: {0} requested non-existing entry {1} in datastore: {2}", _session.GetPlayerInfo(), record.RecordID, dbQuery.TableHash);
				dbReply.Timestamp = (uint)_gameTime.GetGameTime;
			}

            _session.SendPacket(dbReply);
		}
	}

    [WorldPacketHandler(ClientOpcodes.HotfixRequest, Status = SessionStatus.Authed)]
	void HandleHotfixRequest(HotfixRequest hotfixQuery)
	{
        HotfixConnect hotfixQueryResponse = new();

		foreach (var hotfixId in hotfixQuery.Hotfixes)
		{
			var hotfixRecords = _hotfixData.LookupByKey(hotfixId);

			if (hotfixRecords != null)
				foreach (var hotfixRecord in hotfixRecords)
				{
					HotfixConnect.HotfixData hotfixData = new();
					hotfixData.Record = hotfixRecord;

					if (hotfixRecord.HotfixStatus == HotfixRecord.Status.Valid)
					{
						var storage = _dB2Manager.GetStorage(hotfixRecord.TableHash);

						if (storage != null && storage.HasRecord((uint)hotfixRecord.RecordID))
						{
							var pos = hotfixQueryResponse.HotfixContent.GetSize();
							storage.WriteRecord((uint)hotfixRecord.RecordID, _session.SessionDbcLocale, hotfixQueryResponse.HotfixContent);


							if (_optionalData[(int)_session.SessionDbcLocale].TryGetValue((hotfixRecord.TableHash, hotfixRecord.RecordID), out var optionalDataEntries))
								foreach (var optionalData in optionalDataEntries)
								{
									hotfixQueryResponse.HotfixContent.WriteUInt32(optionalData.Key);
									hotfixQueryResponse.HotfixContent.WriteBytes(optionalData.Data);
								}

							hotfixData.Size = hotfixQueryResponse.HotfixContent.GetSize() - pos;
						}
						else
						{
							if (_hotfixBlobData[(int)_session.SessionDbcLocale].TryGetValue((hotfixRecord.TableHash, hotfixRecord.RecordID), out var blobData))
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

		_session.SendPacket(hotfixQueryResponse);
	}
}
