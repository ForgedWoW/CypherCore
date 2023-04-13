// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Framework.Constants;
using Framework.Dynamic;

namespace Forged.MapServer.DataStorage.ClientReader;

internal class WDC3Row
{
    private readonly ColumnMetaData[] _columnMeta;
    private readonly Dictionary<int, Value32>[] _commonData;
    private readonly BitReader _data;
    private readonly bool _dataHasId;
    private readonly int _dataOffset;
    private readonly FieldMetaData[] _fieldMeta;
    private readonly Value32[][] _palletData;
    private readonly int _recordsOffset;
    private readonly int _refId;
    private readonly Dictionary<long, string> _stringsTable;

    public WDC3Row(DBReader reader, BitReader data, int recordsOffset, int id, int refId, Dictionary<long, string> stringsTable)
    {
        _data = data;
        _recordsOffset = recordsOffset;
        _refId = refId;

        _dataOffset = _data.Offset;

        _fieldMeta = reader.FieldMeta;
        _columnMeta = reader.ColumnMeta.ToArray();
        _palletData = reader.PalletData;
        _commonData = reader.CommonData;
        _stringsTable = stringsTable;

        if (id != -1)
        {
            Id = id;
        }
        else
        {
            var idFieldIndex = reader.Header.IdIndex;
            _data.Position = _columnMeta[idFieldIndex].RecordOffset;

            Id = GetFieldValue<int>(idFieldIndex);
            _dataHasId = true;
        }
    }

    public int Id { get; set; }
    public T As<T>() where T : new()
    {
        _data.Position = 0;
        _data.Offset = _dataOffset;

        var fieldIndex = 0;
        T obj = new();

        foreach (var f in typeof(T).GetFields())
        {
            var type = f.FieldType;

            if (f.Name == "Id" && !_dataHasId)
            {
                f.SetValue(obj, (uint)Id);

                continue;
            }

            if (fieldIndex >= _fieldMeta.Length)
            {
                if (_refId != -1)
                    f.SetValue(obj, (uint)_refId);

                continue;
            }

            if (type.IsArray)
            {
                var arrayElementType = type.GetElementType();

                if (arrayElementType.IsEnum)
                    arrayElementType = arrayElementType.GetEnumUnderlyingType();

                var atr = (Array)f.GetValue(obj);

                switch (Type.GetTypeCode(arrayElementType))
                {
                    case TypeCode.SByte:
                        f.SetValue(obj, GetFieldValueArray<sbyte>(fieldIndex, atr.Length));

                        break;
                    case TypeCode.Byte:
                        f.SetValue(obj, GetFieldValueArray<byte>(fieldIndex, atr.Length));

                        break;
                    case TypeCode.Int16:
                        f.SetValue(obj, GetFieldValueArray<short>(fieldIndex, atr.Length));

                        break;
                    case TypeCode.UInt16:
                        f.SetValue(obj, GetFieldValueArray<ushort>(fieldIndex, atr.Length));

                        break;
                    case TypeCode.Int32:
                        f.SetValue(obj, GetFieldValueArray<int>(fieldIndex, atr.Length));

                        break;
                    case TypeCode.UInt32:
                        f.SetValue(obj, GetFieldValueArray<uint>(fieldIndex, atr.Length));

                        break;
                    case TypeCode.Int64:
                        f.SetValue(obj, GetFieldValueArray<long>(fieldIndex, atr.Length));

                        break;
                    case TypeCode.UInt64:
                        f.SetValue(obj, GetFieldValueArray<ulong>(fieldIndex, atr.Length));

                        break;
                    case TypeCode.Single:
                        f.SetValue(obj, GetFieldValueArray<float>(fieldIndex, atr.Length));

                        break;
                    case TypeCode.String:
                        var array = new string[atr.Length];

                        if (_stringsTable == null)
                        {
                            for (var i = 0; i < array.Length; i++)
                                array[i] = _data.ReadCString();
                        }
                        else
                        {
                            var pos = _recordsOffset + (_data.Position >> 3);

                            var strIdx = GetFieldValueArray<int>(fieldIndex, atr.Length);

                            for (var i = 0; i < array.Length; i++)
                                array[i] = _stringsTable.LookupByKey(pos + i * 4 + strIdx[i]);
                        }

                        f.SetValue(obj, array);

                        break;
                    case TypeCode.Object:
                        if (arrayElementType == typeof(Vector3))
                        {
                            var pos = GetFieldValueArray<float>(fieldIndex, atr.Length * 3);

                            var vectors = new Vector3[atr.Length];

                            for (var i = 0; i < atr.Length; ++i)
                                vectors[i] = new Vector3(pos[i * 3], pos[i * 3 + 1], pos[i * 3 + 2]);

                            f.SetValue(obj, vectors);
                        }

                        break;
                    default:
                        throw new Exception("Unhandled array type: " + arrayElementType.Name);
                }
            }
            else
            {
                if (type.IsEnum)
                    type = type.GetEnumUnderlyingType();

                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Single:
                        f.SetValue(obj, GetFieldValue<float>(fieldIndex));

                        break;
                    case TypeCode.Int64:
                        f.SetValue(obj, GetFieldValue<long>(fieldIndex));

                        break;
                    case TypeCode.UInt64:
                        f.SetValue(obj, GetFieldValue<ulong>(fieldIndex));

                        break;
                    case TypeCode.Int32:
                        f.SetValue(obj, GetFieldValue<int>(fieldIndex));

                        break;
                    case TypeCode.UInt32:
                        f.SetValue(obj, GetFieldValue<uint>(fieldIndex));

                        break;
                    case TypeCode.Int16:
                        f.SetValue(obj, GetFieldValue<short>(fieldIndex));

                        break;
                    case TypeCode.UInt16:
                        f.SetValue(obj, GetFieldValue<ushort>(fieldIndex));

                        break;
                    case TypeCode.Byte:
                        f.SetValue(obj, GetFieldValue<byte>(fieldIndex));

                        break;
                    case TypeCode.SByte:
                        f.SetValue(obj, GetFieldValue<sbyte>(fieldIndex));

                        break;
                    case TypeCode.String:
                        if (_stringsTable == null)
                        {
                            f.SetValue(obj, _data.ReadCString());
                        }
                        else
                        {
                            var pos = _recordsOffset + (_data.Position >> 3);
                            var ofs = GetFieldValue<int>(fieldIndex);
                            f.SetValue(obj, _stringsTable.LookupByKey(pos + ofs));
                        }

                        break;
                    case TypeCode.Object:
                        if (type == typeof(LocalizedString))
                        {
                            LocalizedString localized = new();

                            if (_stringsTable == null)
                            {
                                localized[Locale.enUS] = _data.ReadCString();
                            }
                            else
                            {
                                var pos = _recordsOffset + (_data.Position >> 3);
                                var ofs = GetFieldValue<int>(fieldIndex);
                                localized[Locale.enUS] = _stringsTable.LookupByKey(pos + ofs);
                            }

                            f.SetValue(obj, localized);
                        }
                        else if (type == typeof(Vector2))
                        {
                            var pos = GetFieldValueArray<float>(fieldIndex, 2);
                            f.SetValue(obj, new Vector2(pos));
                        }
                        else if (type == typeof(Vector3))
                        {
                            var pos = GetFieldValueArray<float>(fieldIndex, 3);
                            f.SetValue(obj, new Vector3(pos));
                        }
                        else if (type == typeof(FlagArray128))
                        {
                            var flags = GetFieldValueArray<uint>(fieldIndex, 4);
                            f.SetValue(obj, new FlagArray128(flags));
                        }

                        break;
                }
            }

            fieldIndex++;
        }

        return obj;
    }

    public WDC3Row Clone()
    {
        return (WDC3Row)MemberwiseClone();
    }

    private T GetFieldValue<T>(int fieldIndex) where T : unmanaged
    {
        var columnMeta = _columnMeta[fieldIndex];

        switch (columnMeta.CompressionType)
        {
            case DB2ColumnCompression.None:
                var bitSize = 32 - _fieldMeta[fieldIndex].Bits;

                if (bitSize > 0)
                    return _data.Read<T>(bitSize);
                else
                    return _data.Read<T>(columnMeta.Immediate.BitWidth);
            case DB2ColumnCompression.Immediate:
                return _data.Read<T>(columnMeta.Immediate.BitWidth);
            case DB2ColumnCompression.SignedImmediate:
                return _data.ReadSigned<T>(columnMeta.Immediate.BitWidth);
            case DB2ColumnCompression.Common:
                if (_commonData[fieldIndex].TryGetValue(Id, out var val))
                    return val.As<T>();
                else
                    return columnMeta.Common.DefaultValue.As<T>();
            case DB2ColumnCompression.Pallet:
            case DB2ColumnCompression.PalletArray:
                var palletIndex = _data.Read<uint>(columnMeta.Pallet.BitWidth);

                return _palletData[fieldIndex][palletIndex].As<T>();
        }

        throw new Exception(string.Format("Unexpected compression type {0}", _columnMeta[fieldIndex].CompressionType));
    }

    private T[] GetFieldValueArray<T>(int fieldIndex, int arraySize) where T : unmanaged
    {
        var columnMeta = _columnMeta[fieldIndex];

        switch (columnMeta.CompressionType)
        {
            case DB2ColumnCompression.None:
                var bitSize = 32 - _fieldMeta[fieldIndex].Bits;

                var arr1 = new T[arraySize];

                for (var i = 0; i < arr1.Length; i++)
                    if (bitSize > 0)
                        arr1[i] = _data.Read<T>(bitSize);
                    else
                        arr1[i] = _data.Read<T>(columnMeta.Immediate.BitWidth);

                return arr1;
            case DB2ColumnCompression.Immediate:
                var arr2 = new T[arraySize];

                for (var i = 0; i < arr2.Length; i++)
                    arr2[i] = _data.Read<T>(columnMeta.Immediate.BitWidth);

                return arr2;
            case DB2ColumnCompression.SignedImmediate:
                var arr3 = new T[arraySize];

                for (var i = 0; i < arr3.Length; i++)
                    arr3[i] = _data.ReadSigned<T>(columnMeta.Immediate.BitWidth);

                return arr3;
            case DB2ColumnCompression.PalletArray:
                var cardinality = columnMeta.Pallet.Cardinality;

                if (arraySize != cardinality)
                    throw new Exception("Struct missmatch for pallet array field?");

                var palletArrayIndex = _data.Read<uint>(columnMeta.Pallet.BitWidth);

                var arr4 = new T[cardinality];

                for (var i = 0; i < arr4.Length; i++)
                    arr4[i] = _palletData[fieldIndex][i + cardinality * (int)palletArrayIndex].As<T>();

                return arr4;
        }

        throw new Exception(string.Format("Unexpected compression type {0}", columnMeta.CompressionType));
    }
}