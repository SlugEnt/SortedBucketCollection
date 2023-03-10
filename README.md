# SortedBucketCollection
This is a straight copy from Peter Huber SG - https://www.codeproject.com/Articles/5317083/SortedBucketCollection-A-memory-efficient-SortedLi 
Felt the library was useful enough to have it available on Nuget. That's all this is.  I have at this time made no changes to any of the code other than it is set for .Net7

I have also selectively copied his article text to provide some information on how to use, etc.  For the full content of his write-up please see the link above.

## Introduction
Author:  Peter Huber SG. December 2021.  https://www.codeproject.com/Articles/5317083/SortedBucketCollection-A-memory-efficient-SortedLi

I'm writing an accounting software for my own use and needed to store some financial transactions per day. An easy way to store them would be Dictionary<DateTime, FinanceTransaction>, but this allows to store only one transaction per day, however I need to store several transactions a day. This can be achieved by using Dictionary<DateTime, List<FinanceTransaction>>, which has one big disadvantage, it would create thousands of Lists, since I need to use many of these collections, not just one.

I wrote SortedBucketCollection<TKey1, TKey2, TValue> instead. I imagine that there is a bucket for each TKey1 and each bucket can contain several items with unique Tkey2. However, there is not actually a bucket, just a linked list, which doesn't need any memory space like a List does.

For my finance application, I use it like this: SortedBucketCollection<DateTime, int, FinanceTransaction>. TKey1 is the date of the transaction and Tkey2 is the transaction number. Using just the transaction number as key, which is unique, is not good enough, it would be too time consuming to search or sort all transactions whenever the transactions of one day are needed.

 <code>
 using System;

namespace SortedBucketCollectionDemo {

  public record FinanceTransaction
  (int No, DateTime Date, string Description, decimal Amount);

  class Program {
    static void Main(string[] args) {
      //Constructing a SortedBucketCollection
      var transactions = 
        new SortedBucketCollection<DateTime, int, FinanceTransaction>
                                  (ft=>ft.Date, ft=>ft.No);
      var date1 = DateTime.Now.Date;

      //Adding an item to SortedBucketCollection
      transactions.Add(new FinanceTransaction(3, date1, "1.1", 1m));
      transactions.Add(new FinanceTransaction(1, date1, "1.2", 2m));
      transactions.Add(new FinanceTransaction(0, date1, "1.3", 3m));
      var date2 = date1.AddDays(-1);
      transactions.Add(new FinanceTransaction(1, date2, "2.1", 4m));
      transactions.Add(new FinanceTransaction(2, date2, "2.2", 5m));

      //Looping over all items in a SortedBucketCollection
      Console.WriteLine("foreach over all transactions");
      foreach (var transaction in transactions) {
        Console.WriteLine(transaction.ToString());
      }

      //Accessing one particular transaction
      var transaction12 = transactions[date1, 1];

      //Removing  a transaction
      transactions.Remove(transaction12!);

      //Accessing all items of one day
      Console.WriteLine();
      Console.WriteLine("foreach over transactions of one day");
      Console.WriteLine(date1);
      foreach (var transaction in transactions[date1]) {
        Console.WriteLine(transaction.ToString());
      }
    }
  }
}
  </code>
  
Creating a SortedBucketCollection
Definition of SortedBucketCollection class:

C#
public class SortedBucketCollection<TKey1, TKey2, TValue>: 
  ICollection<TValue>, IReadOnlySortedBucketCollection<TKey1, TKey2, TValue>
  where TKey1 : notnull, IComparable<TKey1>
  where TKey2 : notnull, IComparable<TKey2>
  where TValue : class {
The SortedBucketCollection supports two keys. The first is used to group the stored items of type TValue together. The second key is used to uniquely identify an item within that group (bucket).

SortedBucketCollection supports ICollection and can be used as any other collection. Unfortunately, it is not possible to implement IList<>, which allows access of an item based on its position within the list. SortedBucketCollection cannot implement this efficiently.

The constructor of SortedBucketCollection looks like this:

C#
public SortedBucketCollection(Func<TValue, TKey1> getKey1, Func<TValue, TKey2> getKey2) {}
It takes two parameters, the delegates getKey1 and getKey2. I never liked that in Dictionary<> and SortedList<> both the key and the item need to be passed, because most of the time, the key is already a property of the item. To improve this, I added these two delegates which allow to find Key1 and Key2 in the item.

Using the constructor:

C#
var transactions = 
  new SortedBucketCollection<DateTime, int, FinanceTransaction> (ft=>ft.Date, ft=>ft.No)
In the demo, a FinanceTransaction has a date and a transaction number, which are used as the two required keys.

C#
public record FinanceTransaction(int No, DateTime Date, string Description, decimal Amount);
Adding an Item to SortedBucketCollection
Adding an item becomes very simple:

C#
transactions.Add(new FinanceTransaction(uniqueNo++, date, "Some remarks", 10.00m));
No key needs to be given as a separate parameter. SortedBucketCollection knows how to find the keys already. It takes the first key, a date and checks if there is already any item with the same date. If not, it just adds the new item under that date. If there is already one, SortedBucketCollection checks if the new item should come before or after the existing item. After Add() is completed, the two items are stored in a properly sorted linked list.

Note: SortedBucketCollection needs to increase its storage capacity only for the single SortedDirectory, which happens seldom. With the Dictionary<DateTime, List<FinanceTransaction>> approach, thousands of lists have to increase their capacity constantly (i.e., copy the internal array into a bigger array).

Looping Over All Items in a SortedBucketCollection
Nothing special here, as any other collection, SortedBucketCollection supports IEnumerable(TValue):

C#
foreach (var transaction in transactions) {
  Console.WriteLine(transaction.ToString());
}
The output is as follows:

FinanceTransaction { No = 1, Date = 07.11.2021 00:00:00, Description = 2.1, Amount = 4 }
FinanceTransaction { No = 2, Date = 07.11.2021 00:00:00, Description = 2.2, Amount = 5 }
FinanceTransaction { No = 0, Date = 08.11.2021 00:00:00, Description = 1.3, Amount = 3 }
FinanceTransaction { No = 1, Date = 08.11.2021 00:00:00, Description = 1.2, Amount = 2 }
FinanceTransaction { No = 3, Date = 08.11.2021 00:00:00, Description = 1.1, Amount = 1 }
The items are not in the sequence as they were added, as would be the case with a List<>, but sorted first by date and then by No.

Accessing One Particular Item
C#
var transaction = transactions[new DateTime(2021, 11, 07), 1];
Removing an Item
C#
transactions.Remove(transaction);
In a Dictionary<>, one would use the key to remove the item. SortedBucketCollection can figure out the key based on the item given.

Accessing All Items of One Day
C#
var date1Transaction1 = transactions[date1];
This is where SortedBucketCollection truly shines. One could store all transactions in a List<> and use Linq to find all transactions with a certain date and then sort them according to transaction.No. This would be a rather time consuming operation. When writing my financial application, I found out that I needed to access transactions per day very often and that is the reason I wrote SortedBucketCollection.  
