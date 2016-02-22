
<img  width="60%" src="http://markmeeus.github.io/MarcelloDB/images/logo/logo_blue.svg"/>

[![Build Status](https://travis-ci.org/markmeeus/MarcelloDB.svg?branch=master)](https://travis-ci.org/markmeeus/MarcelloDB)

MarcelloDB is a mobile NoSql database.
It is very light-weight with minimal memory footprint.

MarcelloDB saves plain C# objects, including child objects, lists and collections.
Not having to map your objects to the relational model can save you hundreds of lines of code.

MarcelloDB is a pure C# implementation, so there is no need to package other binaries.

MarcelloDB is portable code: Xamarin (iOS and Android), Windows 8.1 and Windows Phone 8.1 and Windows 10



Current Status
=
Marcello is still in an experimental phase.

Be carefull. Backwards compatibility with existing data will not be guaranteed untill v1.

Getting Started
````
PM > Install-Package MarcelloDB
```
Sessions
=
Using MarcelloDB starts with the creation of a session.
The session makes sure you have access to the actual files where the data is stored.
A session handles a set of data files in a specific folder.

MarcelloDB is a portable class library, but there are some platform specific things that simply cannot be written in portable code. Currently, MarcelloDB relies on a platform specific implementation for interacting with the file system.

All of this is hidden from you as it is wrapped inside the Platform object.

The Platform implementation for mono/.net which can be used for Xamarin iOS and Android can be found in the Marcello.netfx assembly.

The implementation for Windows Phone 8.1 and Windows 8.1 (not tested in Windows 10 yet) can be found in the Marcello.uwp assembly.

So first: create the Platform object in your platform specific project.

```cs
//Create a platform on the specific platform
var platform =  new Platform();
```

Then create the actual session.
(You can do this in a portable class library)
```cs
//Create a Marcello session, this can be done in PCL code
var session = new MarcelloDB.Session(platform, "/path/to/data/folder/");
```

CollectionFiles and Collections
=
MarcelloDB organizes it's data in collection-files and collections. A single session can handle multiple collection-files and a collection-file can handle multiple collections.

(This way you can have all read-only data in a single file which you can download straight from your servers. Another file could be used to write data f.i.)

You access collection-files like this:
```cs
var productsFile = session["products"];
```

With this collection-file, you can start accessing collections.
Collections are a bit like tables, the main difference is that they contain entire objects, not just rows with colums.
A collection can only handle objects of a specific type (including subclasses).

You can get the collection form the collection-file like this:
```cs
//Deals with instances of Book or subclasses of Book
var bookCollection = productsFile.Collection<Book>("books");
var dvdCollection = productsFile.Collection<Dvd>("dvd");
```
If you use different collection-names you can even have multiple collections for the same type within one collection-file.
```cs
//Deals with instances of Book or subclasses of Book
var dvdCollection = productsFile.Collection<Dvd>("dvds");
var upcommingDvdCollection = productsFile.Collection<Dvd>("upcomming-dvds");
```

Persisting objects
=
This is where it gets really easy. Once you have this collection, just throw an instance at it:
```cs
var book = new Book(){ ID = "123",  Title = "The Girl With The Dragon Tattoo" };
bookCollection.Persist(book);
```

Enumerating a collection
=
A collection exposes an All property which implements IEnumerable.
You can use it iterate all objects in the collection, and of course use Linq on it.
(Note that when using Linq, it is still enumerating the collection)
```cs
foreach(var book in bookCollection.All){}
```

Finding objects
=
You can find a specific object by it's ID (more on ID's below)

Find uses a btree index to search for objects. Depending on the size of your collection, a Find will be way faster than iterating the collection to find it.
Sub-millisecond lookups should be expected, even for large collections.

Usage:

```cs
var book = bookCollection.Find(123)
```

Indexes
=

MarcelloDB allows you to define indexes on your data. Using these indexes, finding an object will be super fast.
To enable indexes on a collection, you need to create it with an index definition.
For instance:
```cs
productsFile.Collection<Dvd, DvdIndexDefiniton>("dvds");
```

An index definition has to be based on the MarcelloDB.Index.IndexDefinition<T> subclass, where T is the type of the objects you want to store.
In case of the dvdCollection:

```cs
class DvdIndexDefinition : IndexDefinition<Dvd>{}
```

Indexing properties
==
The simplest way to index a property (of Dvd in this case) is to define a property on the index definition.

It has to 
* have the same name as the property you want to index
* be of type IndexedValue<TObj, TAttribute>
* have the same type for TAttribute as the type of the property you want to index
* has to return null when instantiated
* Have a working setter (so that get returns what has been set)

If any of these requirements is not met, the Collection method on the collection-file will throw as soon as you try to access it. This way, you either get a valid collection, or it throws.

An example:
```cs
///
/// This IndexDefinition can be used to index the title property of Dvd's
///
class DvdIndexDefinition : IndexDefinition<Dvd>
{
   //                               Index title
   //                                 ||
   public  IndexedValue<Dvd, string> Title { get; set; }
   //                   ||     ||
   //             of Dvd which is a string 
}
```

If indexing a property is not enough, you can also define custom indexes.
In this case you also define a property with the same type requirements as above, except, you return an instance of IndexedValue. (And a setter is not needed)

Like this:
```cs
///
/// This IndexDefinition can be used to index the main categoruy property of Dvd's
///
class DvdIndexDefinition : IndexDefinition<Dvd>
{
   
   public  IndexedValue<Dvd, string> MainCategory
   { 
      get{
        //index the first 3 characters of the Category
        return base.IndexedValue<Dvd, string>((dvd)=>dvd.Category.SubString(0,3));
      }
   }
}
```

If it is comparable, it can be indexed
==
If a type implements IComparable, it can be indexed. Yes, even your custom objects. Just make sure they compare ok
```cs
class DvdIndexDefinition : IndexDefinition<Dvd>
{
  public  IndexedValue<Dvd, Rating> Rating { get; set; }
}
```


Deleting objects
=
Delete your objects like this:
```cs
bookCollection.Destroy(book.ID);
```

How Objects are Identified
=
MarcelloDB needs a single property which uniquely identifies an object.
This value is needed to build the main index and make a distinction between an insert or an update.

You can use any of the following naming conventions, or add the MarcelloDB.Attributes.ID attribute.
```cs
class Article
{
  public string ID { get; set; }

  public string Id { get; set; }

  public string id { get; set; }

  public string ArticleID { get; set; }

  public string ArticleId { get; set; }

  public string Articleid { get; set; }

  [MarcelloDB.Attributes.ID]
  public string CustomIDProp { get; set; }
}
```
The ID, Id and id property can be defined anywhere in the class hierarchy, but the ClassID, ClassId and Classid attributes will only be used if they are defined on the class which is used as the collection class.

```cs
class Product
{
  public string ID { get; set; }
}
class Chair : Product{}

//This will work
session["data"].Collection<Product>("products").Persist(chair);

//This will also work because an ID (or Id or id) property is found in the class hiërachy.
session["data"].Collection<Chair>("chairs").Persist(chair);


class Article
{
  public string ArticleID { get; set; }
}
class Book : Article{}

//This will work
session["data"].Collection<Article>("articles").Persist(book);

//This will not work because Book doesn't define an ID and ArticleID on the parent isn't considered.
session["data"].Collection<Book>("books").Persist(book);
```

Transactions
=
To avoid data corruption, all changes are written to a write ahead journal and applied as a single atomic and durable action.
MarcelloDB does this for calls to Persist and Destroy, but you can extend the transaction to make it span multiple data mutations.

A transaction runs on a session and can include changes in multiple collections from multiple collection files.

(Warning: only collections obtained from that session will be included in the transaction. If you start to mix multiple sessions, you're on your own.)
```cs
session.Transaction(() => {
    session["articles.dat"].Collection<Article>("articles").Persist(article);
    session["project_management.dat"].Collection<Client>("clients").Persist(client);
    session["project_management.dat"].Collection<Project>("projects").Destroy(project);
});
```

Transactions roll back when an exception occurs within the block.
```cs
session.Transaction(() => {
    session["articles.dat"].Collection<Article>("articles").Persist(article);
    session["project_management.dat"].Collection<Client>("clients").Persist(client);
    session["project_management.dat"].Collection<Project>("projects").Destroy(project);
    throw new Exception("Nothing happened");
});
```

Encryption
=
Data is stored in an unencrypted format. An encryption engine is available as a pro extension. Please contact mark.meeus at gmail.com for more info.

Indexes
=
Will be implemented in 0.2.x and 0.3.x

Roadmap
=
0.1.0
-
- ~~Persisting objects~~
- ~~Deleting objects~~
- ~~enumerating objects~~
- ~~reusing storage of deleted objects~~
- ~~cross-collection transaction support~~
- ~~Indexing~~
- ~~iOS/Android (Xamarin) support~~
- ~~Polymorphic collections with Polymorphic children~~
- ~~Windows 8.1 Support~~
- ~~Windows Phone 8.1 Support~~

0.2.0
-
- ~~Indexing properties~~
- ~~Indexing user-defined values~~
- ~~Iterating indexes~~
- ~~Iterating a range from an index~~

0.3.0
-
- Iterating indexes in descending order
- Iterating index keys only

0.4.0
-
- Indexing list of values
- Contains/ContainsAll query

0.5.0
-
- Unique index attribute
- IncludeNull index attribute

1.0.0
-
- Stable file format, to be supported in all future 1.x versions






