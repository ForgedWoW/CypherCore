// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Framework.Constants;

namespace Forged.RealmServer.DataStorage;

class DBReader
{
	public WDCHeader Header;
	public FieldMetaData[] FieldMeta;
	public ColumnMetaData[] ColumnMeta;
	public Value32[][] PalletData;
	public Dictionary<int, Value32>[] CommonData;

	public Dictionary<int, WDC3Row> Records = new();
	private const uint WDC3FmtSig = 0x33434457; // WDC3

	public bool Load(Stream stream)
	{
		using (var reader = new BinaryReader(stream, Encoding.UTF8))
		{
			Header = new WDCHeader();
			Header.Signature = reader.ReadUInt32();

			if (Header.Signature != WDC3FmtSig)
				return false;

			Header.RecordCount = reader.ReadUInt32();
			Header.FieldCount = reader.ReadUInt32();
			Header.RecordSize = reader.ReadUInt32();
			Header.StringTableSize = reader.ReadUInt32();
			Header.TableHash = reader.ReadUInt32();
			Header.LayoutHash = reader.ReadUInt32();
			Header.MinId = reader.ReadInt32();
			Header.MaxId = reader.ReadInt32();
			Header.Locale = reader.ReadInt32();
			Header.Flags = (HeaderFlags)reader.ReadUInt16();
			Header.IdIndex = reader.ReadUInt16();
			Header.TotalFieldCount = reader.ReadUInt32();
			Header.BitpackedDataOffset = reader.ReadUInt32();
			Header.LookupColumnCount = reader.ReadUInt32();
			Header.ColumnMetaSize = reader.ReadUInt32();
			Header.CommonDataSize = reader.ReadUInt32();
			Header.PalletDataSize = reader.ReadUInt32();
			Header.SectionsCount = reader.ReadUInt32();

			var sections = reader.ReadArray<SectionHeader>(Header.SectionsCount);

			// field meta data
			FieldMeta = reader.ReadArray<FieldMetaData>(Header.FieldCount);

			// column meta data 
			ColumnMeta = reader.ReadArray<ColumnMetaData>(Header.FieldCount);

			// pallet data
			PalletData = new Value32[ColumnMeta.Length][];

			for (var i = 0; i < ColumnMeta.Length; i++)
				if (ColumnMeta[i].CompressionType == DB2ColumnCompression.Pallet || ColumnMeta[i].CompressionType == DB2ColumnCompression.PalletArray)
					PalletData[i] = reader.ReadArray<Value32>(ColumnMeta[i].AdditionalDataSize / 4);

			// common data
			CommonData = new Dictionary<int, Value32>[ColumnMeta.Length];

			for (var i = 0; i < ColumnMeta.Length; i++)
				if (ColumnMeta[i].CompressionType == DB2ColumnCompression.Common)
				{
					Dictionary<int, Value32> commonValues = new();
					CommonData[i] = commonValues;

					for (var j = 0; j < ColumnMeta[i].AdditionalDataSize / 8; j++)
						commonValues[reader.ReadInt32()] = reader.Read<Value32>();
				}

			long previousRecordCount = 0;

			for (var sectionIndex = 0; sectionIndex < Header.SectionsCount; sectionIndex++)
			{
				if (sections[sectionIndex].TactKeyLookup != 0) // && !hasTactKeyFunc(sections[sectionIndex].TactKeyLookup))
				{
					previousRecordCount += sections[sectionIndex].NumRecords;

					//Console.WriteLine("Detected db2 with encrypted section! HasKey {0}", CASC.HasKey(Sections[sectionIndex].TactKeyLookup));
					continue;
				}

				reader.BaseStream.Position = sections[sectionIndex].FileOffset;

				byte[] recordsData;
				Dictionary<long, string> stringsTable = null;
				SparseEntry[] sparseEntries = null;

				if (!Header.HasOffsetTable())
				{
					// records data
					recordsData = reader.ReadBytes((int)(sections[sectionIndex].NumRecords * Header.RecordSize));

					// string data
					stringsTable = new Dictionary<long, string>();

					for (var i = 0; i < sections[sectionIndex].StringTableSize;)
					{
						var oldPos = reader.BaseStream.Position;

						stringsTable[i] = reader.ReadCString();

						i += (int)(reader.BaseStream.Position - oldPos);
					}
				}
				else
				{
					// sparse data with inlined strings
					recordsData = reader.ReadBytes(sections[sectionIndex].SparseTableOffset - sections[sectionIndex].FileOffset);

					if (reader.BaseStream.Position != sections[sectionIndex].SparseTableOffset)
						throw new Exception("reader.BaseStream.Position != sections[sectionIndex].SparseTableOffset");
				}

				Array.Resize(ref recordsData, recordsData.Length + 8); // pad with extra zeros so we don't crash when reading

				// index data
				var indexData = reader.ReadArray<int>((uint)(sections[sectionIndex].IndexDataSize / 4));
				var isIndexEmpty = Header.HasIndexTable() && indexData.Count(i => i == 0) == sections[sectionIndex].NumRecords;

				// duplicate rows data
				Dictionary<int, int> copyData = new();

				for (var i = 0; i < sections[sectionIndex].NumCopyRecords; i++)
					copyData[reader.ReadInt32()] = reader.ReadInt32();

				if (sections[sectionIndex].NumSparseRecords > 0)
					sparseEntries = reader.ReadArray<SparseEntry>((uint)sections[sectionIndex].NumSparseRecords);

				// reference data
				ReferenceData refData = null;

				if (sections[sectionIndex].ParentLookupDataSize > 0)
				{
					refData = new ReferenceData
					{
						NumRecords = reader.ReadInt32(),
						MinId = reader.ReadInt32(),
						MaxId = reader.ReadInt32()
					};

					refData.Entries = new Dictionary<int, int>();
					var entries = reader.ReadArray<ReferenceEntry>((uint)refData.NumRecords);

					foreach (var entry in entries)
						refData.Entries[entry.Index] = entry.Id;
				}
				else
				{
					refData = new ReferenceData
					{
						Entries = new Dictionary<int, int>()
					};
				}

				if (sections[sectionIndex].NumSparseRecords > 0)
				{
					// TODO: use this shit
					var sparseIndexData = reader.ReadArray<int>((uint)sections[sectionIndex].NumSparseRecords);

					if (Header.HasIndexTable() && indexData.Length != sparseIndexData.Length)
						throw new Exception("indexData.Length != sparseIndexData.Length");

					indexData = sparseIndexData;
				}

				BitReader bitReader = new(recordsData);

				for (var i = 0; i < sections[sectionIndex].NumRecords; ++i)
				{
					bitReader.Position = 0;

					if (Header.HasOffsetTable())
						bitReader.Offset = sparseEntries[i].Offset - sections[sectionIndex].FileOffset;
					else
						bitReader.Offset = i * (int)Header.RecordSize;

					var hasRef = refData.Entries.TryGetValue(i, out var refId);

					var recordIndex = i + previousRecordCount;
					var recordOffset = (recordIndex * Header.RecordSize) - (Header.RecordCount * Header.RecordSize);

					var rec = new WDC3Row(this, bitReader, (int)recordOffset, Header.HasIndexTable() ? (isIndexEmpty ? i : indexData[i]) : -1, hasRef ? refId : -1, stringsTable);
					Records.Add(rec.Id, rec);
				}

				foreach (var copyRow in copyData)
					if (copyRow.Key != 0)
					{
						var rec = Records[copyRow.Value].Clone();
						rec.Id = copyRow.Key;
						Records.Add(copyRow.Key, rec);
					}

				previousRecordCount += sections[sectionIndex].NumRecords;
			}
		}

		return true;
	}
}