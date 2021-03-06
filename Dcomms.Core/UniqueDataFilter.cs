﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dcomms
{
    /// <summary>
    /// single-threaded usage only!!!
    /// MAY return false-positives in case when 32-bit hashcode (defined inside this module) returns same value
    /// </summary>
    class UniqueDataFilter
    {
        LinkedList<int> _recentHashcodes = new LinkedList<int>(); // newest=latest
        HashSet<int> _recentHashcodesHS = new HashSet<int>();
        readonly int _maxRecentItemsToKeep;
        public UniqueDataFilter(int maxRecentItemsToKeep)
        {
            _maxRecentItemsToKeep = maxRecentItemsToKeep;
        }
        public bool Filter(Action<BinaryWriter> writeUniqueFields)
        {
            BinaryProcedures.CreateBinaryWriter(out var ms, out var w);
            writeUniqueFields(w);
            var dataMustBeUnique = ms.ToArray();
            return Filter(dataMustBeUnique);
        }
        public bool DataIsUnique(byte[] data)
        {
            return !_recentHashcodesHS.Contains(MiscProcedures.GetArrayHashCode(data));
        }
        public bool Filter(byte[] dataMustBeUnique)
        {
            var hashCode = MiscProcedures.GetArrayHashCode(dataMustBeUnique);
            if (_recentHashcodesHS.Contains(hashCode)) return false;

            _recentHashcodesHS.Add(hashCode);
            _recentHashcodes.AddLast(hashCode);

            if (_recentHashcodes.Count > _maxRecentItemsToKeep)
            {
                var removedItem = _recentHashcodes.First;
                _recentHashcodes.RemoveFirst();
                _recentHashcodesHS.Remove(removedItem.Value);
            }

            return true;
        }
        public void AssertIsUnique(byte[] dataMustBeUnique, string dataDescription)
        {
            if (!Filter(dataMustBeUnique))
                throw new NonUniquePacketFieldsException($"data is not unique: {dataDescription}");
        }
        public void AssertIsUnique(Action<BinaryWriter> w, string dataDescription)
        {
            if (!Filter(w))
                throw new NonUniquePacketFieldsException($"data is not unique: {dataDescription}");
        }
    }
}
