﻿using System.Collections;
using System.Diagnostics.CodeAnalysis;


namespace SlugEnt;

/// <summary>
/// Like a SortedList, SortedBucketCollection stores TValue items which can be retrieved by their TKey1 value. In SortedList, 
/// each TKey accesses at most 1 item. In SortedBucketCollection, each TKey1 accesses a bucket, which can contain 0 to main items 
/// with the same TKey1, but different, unique TKey2. Items are sorted by TKey1, then TKey2. Tkey1 and TKey2 are 
/// properties within TValue.
/// </summary>
public class SortedBucketCollection<TKey1, TKey2, TValue> : IEnumerable<TValue>, ICollection<TValue>, IReadOnlySortedBucketCollection<TKey1, TKey2, TValue>
    where TKey1 : notnull, IComparable<TKey1>
    where TKey2 : notnull, IComparable<TKey2>
    where TValue : class
{
#region Properties

    //      ----------

    /// <summary>
    /// A collection of all TKey1 values stored.
    /// </summary>
    public ICollection<TKey1> Keys => buckets.Keys;


    /// <summary>
    /// Number of all TValues stored
    /// </summary>
    public int Count { get; private set; }


    /// <summary>
    /// Number of all TKeys stored
    /// </summary>
    public int Key1Count => buckets.Count;


    /// <summary>
    /// Readonly versions of SortedBucketCollection don't exist
    /// </summary>
    public bool IsReadOnly => false;


    /// <summary>
    /// Returns all items with TKey==key1
    /// </summary>
    public IEnumerable<TValue> this[TKey1 key1] => getValuesFor(key1);


    private IEnumerable<TValue> getValuesFor(TKey1 key1)
    {
        if (!buckets.TryGetValue(key1, out var bucketItem))
            yield break;

        var versionCopy = version;
        do
        {
            yield return bucketItem.Item;

            if (versionCopy != version)
            {
                throw new InvalidOperationException();
            }

            bucketItem = bucketItem.Next;
        } while (bucketItem is not null);
    }


    /// <summary>
    /// Returns the item stored for key1, key2
    /// </summary>
    public TValue? this[TKey1 key1, TKey2 key2] => getValueFor(key1, key2);


    private TValue? getValueFor(TKey1 key1, TKey2 key2)
    {
        if (!buckets.TryGetValue(key1, out var bucketItem))
            return null;

        var compareResult = getKey2(bucketItem.Item).CompareTo(key2);
        while (compareResult == -1)
        {
            //Key2 is greater than bucketItem.Key2
            if (bucketItem.Next is null)
                return null;

            bucketItem    = bucketItem.Next;
            compareResult = getKey2(bucketItem.Item).CompareTo(key2);
        }

        return compareResult == 0 ? bucketItem.Item : null;
    }

#endregion


#region BucketItem

    //      ----------


    /// <summary>
    /// Basic storage unit in StoredBuckets.
    /// </summary>
    private class BucketItem
    {
        public readonly TValue      Item;
        public          BucketItem? Next;


        public BucketItem(TValue item, BucketItem? next)
        {
            Item = item;
            Next = next;
        }


        public override string ToString()
        {
            if (Next is null)
            {
                return $"{Item}; Next: ";
            }
            else
            {
                var nextItem = Next.Item; //bug in C# compiler with $"Next: {this.Next?.Item}"
                return $"{Item}; Next: {nextItem}";
            }
        }
    }

#endregion


#region Constructor

    //      -----------

    readonly Func<TValue, TKey1>                 getKey1;
    readonly Func<TValue, TKey2>                 getKey2;
    readonly SortedDictionary<TKey1, BucketItem> buckets;
    int                                          version;


    /// <summary>
    /// The getKeyX() delegates provide access to the TKeyX property in TValue
    /// </summary>
    public SortedBucketCollection(Func<TValue, TKey1> getKey1, Func<TValue, TKey2> getKey2)
    {
        this.getKey1 = getKey1;
        this.getKey2 = getKey2;
        buckets      = new();
        Count        = 0;
    }

#endregion


#region Methods

    //      -------


    /// <summary>
    /// Adds value to SortedBucketCollection. It throws an exception if an item is stored already with value.Key1 and value.Key2.
    /// </summary>
    public void Add(TValue item)
    {
        var key1 = getKey1(item);
        if (buckets.TryGetValue(key1, out var foundBucketItem))
        {
            var key2                = getKey2(item);
            var foundBucketItemKey2 = getKey2(foundBucketItem.Item);
            var compareResult       = key2.CompareTo(foundBucketItemKey2);
            if (compareResult == 0)
                throw new ArgumentException($"SortedBucketCollection.Add({item}): Key2 {key2} is used by already stored {foundBucketItem}.");

            if (compareResult < 0)
            {
                //bucket.Key2<foundBucket.Key2
                //insert bucket into buckets, link foundBucket from bucket
                buckets[key1] = new BucketItem(item, foundBucketItem);
            }
            else
            {
                //bucket.Key2>foundBucket.Key2
                //find where in the linked list bucket needs to get inserted
                var nextBucketItem = foundBucketItem.Next;
                while (nextBucketItem is not null)
                {
                    var nextBucketItemKey2 = getKey2(nextBucketItem.Item);
                    compareResult = key2.CompareTo(nextBucketItemKey2);
                    if (compareResult == 0)
                        throw new ArgumentException($"SortedBucketCollection.Add({item}): Key2 {key2} is used by already stored {nextBucketItem}.");

                    if (compareResult < 0)
                    {
                        //bucket.Key2<nextBucketItem.Key2
                        //insert bucket between foundBucketItem and nextBucketItem
                        foundBucketItem.Next = new BucketItem(item, nextBucketItem);
                        Count++;
                        version++;
                        return;
                    }

                    foundBucketItem     = nextBucketItem;
                    foundBucketItemKey2 = nextBucketItemKey2;
                    nextBucketItem      = foundBucketItem.Next;
                }

                //bucket has bigger Key2 than any exisitng bucktItem with same Key1
                foundBucketItem.Next = new BucketItem(item, null);
            }
        }
        else
        {
            //first item for that Key1
            buckets.Add(key1, new BucketItem(item, null));
        }

        Count++;
        version++;
    }


    /// <summary>
    /// Does at least 1 item exist in SortedBuckers with item.Key1==key1 ?
    /// </summary>
    public bool Contains(TKey1 key1) { return buckets.ContainsKey(key1); }


    /// <summary>
    /// Does an item exist in SortedBuckers with item.Key1==key1 and item.Key2==key2 ?
    /// </summary>
    public bool Contains(TKey1 key1, TKey2 key2) { return this[key1, key2] is not null; }


    /// <summary>
    /// Does an item with item.Key1==key1 and item.Key2==key2 exist in SortedBuckers ?
    /// </summary>
    public bool Contains(TValue item)
    {
        var key1 = getKey1(item);
        var key2 = getKey2(item);
        return this[key1, key2] is not null;
    }


    /// <summary>
    /// Returns true if StoredBuckets stores an item with item.Key1==key1 and item Key2==key2
    /// </summary>
    public bool TryGetValue(TKey1 key1, TKey2 key2, [MaybeNullWhen(false)] out TValue value)
    {
        value = getValueFor(key1, key2);
        return value is not null;
    }


    /// <summary>
    /// Enumerates over all items in SortedBucketCollection, sorted by Key1, Key2
    /// </summary>
    public IEnumerator<TValue> GetEnumerator()
    {
        var versionCopy = version;

        foreach (var keyValuePairBucketItem in buckets)
        {
            var bucketItem = keyValuePairBucketItem.Value;
            yield return bucketItem.Item;

            if (versionCopy != version)
            {
                throw new InvalidOperationException();
            }

            while (bucketItem.Next is not null)
            {
                bucketItem = bucketItem.Next;
                yield return bucketItem.Item;

                if (versionCopy != version)
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }


    /// <summary>
    /// Enumerates over all items in SortedBucketCollection, sorted by Key1, Key2
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }


    /// <summary>
    /// Returns true if an item with item.Key1==key1 and item.Key2==key2 was found. Item gets removed.
    /// </summary>
    public bool Remove(TValue item)
    {
        var key1 = getKey1(item);
        if (!buckets.TryGetValue(key1, out var existingBucketItem))
            return false; //Key1 is completely missing in buckets

        var key2                   = getKey2(item);
        var existingBucketItemKey2 = getKey2(existingBucketItem.Item);
        var compareResult          = key2.CompareTo(existingBucketItemKey2);
        if (compareResult == 0)
        {
            if (existingBucketItem.Next is null)
            {
                //only one item for Key1, remove it
                buckets.Remove(key1);
            }
            else
            {
                //replace found item with its next item
                buckets[key1] = existingBucketItem.Next;
            }

            Count--;
            version++;
            return true;
        }
        else if (compareResult < 0)
        {
            //Key2<existingBucket.Key2, Key2 is missing
            return false;
        }

        //Key2>existingBucket.Key2
        //search linked list bucket for Key2
        var nextBucketItem = existingBucketItem.Next;
        while (nextBucketItem is not null)
        {
            var nextBucketItemKey2 = getKey2(nextBucketItem.Item);
            compareResult = key2.CompareTo(nextBucketItemKey2);
            if (compareResult < 0)
            {
                //Key2<nextBucketItem.Key2
                //Key2 cannot be found
                return false;
            }
            else if (compareResult == 0)
            {
                //item matching Key1 and Key2 found. Remove it from linked list
                existingBucketItem.Next = nextBucketItem.Next;
                Count--;
                version++;
                return true;
            }

            existingBucketItem     = nextBucketItem;
            existingBucketItemKey2 = nextBucketItemKey2;
            nextBucketItem         = existingBucketItem.Next;
        }

        //end of linked list reached, key2 not found
        return false;
    }


    /// <summary>
    /// Releases all items from SortedBucketCollection
    /// </summary>
    public void Clear()
    {
        buckets.Clear(); //by removing the start of every linked list, all other items in the linked list become unreachable too
        Count = 0;
        version++;
    }


    /// <summary>
    /// CopyTo() is not supported by SortedBucketCollection
    /// </summary>
    public void CopyTo(TValue[] array, int arrayIndex) { throw new NotSupportedException(); }

#endregion
}