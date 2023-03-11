/**************************************************************************************

SortedBucketCollection
======================

SortedBucketCollection stores BucketItems. For each unique TKey1, buckets, a SortedDictionary for TKey1, stores exactly 1
BucketItem, which can be the head of a linked list, containing other BucketItems with the same TKey1, sorted 
by TKey2. Within such a linked list, each TKey2 must be unique.

Written in 2021 by Jürgpeter Huber 
Contact: PeterCode at Peterbox dot com

To the extent possible under law, the author(s) have dedicated all copyright and 
related and neighboring rights to this software to the public domain worldwide under
the Creative Commons 0 license (details see COPYING.txt file, see also
<http://creativecommons.org/publicdomain/zero/1.0/>). 

This software is distributed without any warranty. 
**************************************************************************************/
using System.Diagnostics.CodeAnalysis;


namespace SlugEnt;

#region IReadOnlySortedBucketCollection
//      -------------------------------

/// <summary>
/// Provides readonly access to a BucketCollection, which works like a Dictionary, but each item needs 2 keys.
/// </summary>
public interface IReadOnlySortedBucketCollection<TKey1, TKey2, TValue> : IReadOnlyCollection<TValue>
    {

        #region Properties
        //      ----------

        /// <summary>
        /// A collection of all TKey1 values stored.
        /// </summary>
        ICollection<TKey1> Keys { get; }


        /// <summary>
        /// Number of all TKeys stored
        /// </summary>
        public int Key1Count { get; }


        /// <summary>
        /// Returns all items with TKey==key1
        /// </summary>
        public IEnumerable<TValue> this[TKey1 key1] { get; }


        /// <summary>
        /// Returns the item stored for key1, key2
        /// </summary>
        public TValue? this[TKey1 key1, TKey2 key2] { get; }
        #endregion


        #region Methods
        //      -------

        /// <summary>
        /// Does at least 1 item exist in SortedBuckers with item.Key1==key1 ?
        /// </summary>
        public bool Contains(TKey1 key1);
        #endregion


        /// <summary>
        /// Does an item exist in SortedBuckers with item.Key1==key1 and item.Key2==key2 ?
        /// </summary>
        public bool Contains(TKey1 key1, TKey2 key2);


        /// <summary>
        /// Does an item with item.Key1==key1 and item.Key2==key2 exist in SortedBuckers ?
        /// </summary>
        public bool Contains(TValue item);


        /// <summary>
        /// Returns true if StoredBuckets stores an item with item.Key1==key1 and item Key2==key2
        /// </summary>
        public bool TryGetValue(TKey1 key1, TKey2 key2, [MaybeNullWhen(false)] out TValue value);
    }
    #endregion


