# Cascade

Cascade is a generic framework for performing either ad-hoc or reactive queries of data.

## Design Overview

At its core this library is a differential dataflow framework. Dataflow is a form of programming in which processing
is configured via the connecting of nodes into a dataflow graph

. Data is then pushed into the flow, and the graph of
nodes processes the data transforming it until it flows into one of the graph outlets.

A variant of Dataflow is known as "Differential Dataflow". In this model, only diffs or changes to the data are pushed
through the graph. This form of processing often results in more complex operators, but allows for minimal graph updates
when only a subset of the inlets are updated.

In Cascade these diffs are represented by a tuple-like structure called a `Diff<T>` that pairs a value `T` with a delta,
which is a `int`. A positive integer means that the count of the given value is to be increased by that amount, a negative
number results in a decrease of that value.

### Example

Let's say that we are tracking people that enter and exit a room, and we are only interested in their first names. If "Bill"
and "Sally" walk into the room, we would push `["Bill", 1], ["Sally", -1]` into the graph. If Sally exits the room
and another person named Bill entered the room, we would push `["Sally", -1], ["Bill", 1]`. At which point the state
of the room would be `["Bill", 2]`. Any value with a delta of `0` ceases to exist in the graph and is removed.

---

Various Differential Dataflow libraries have different constraints and features. Cascade is no different. `Timely Dataflow` a
Rust-based differential dataflow framework heavily emphasises multi-process and multi-core programming, but does not allow the
graph to be updated at runtime. Essentially changing the structure of the flow requires restarting the flow (and re-flowing all previous
data to get back to a steady state)

Cascade takes a different approach. All modifications to the graph, including the flow of data is done by a single primary
thread. That thread may (later on) divide up work and hand it to other cores, but the key concession is that a single system
of nodes (called a Topology in Cascade) can only be modified by a single thread, messages are sent to this thread and it
will update the topology as required. Thanks to immutable data the outlets of the topology can be read at any time, but modification
and the addition of new data must be queued up for later processing. Methods exist on the Topology for flushing all pending
data making consistent tests much easier.

## Design Constraints
When making Cascade several problems and constraints were taken into account. They are listed below:

### Ad-hoc queries
In a app like the Nexus Mods app, certain queries will only be required at certain times. So a large static flow
is not a workable design constraint. Some queries, like those for diagnostics or specific tools may be used at certain times,
then the query needs to be torn down and the memory freed. This also means that attaching a new flow should reuse existing
operators already in the Topology. New nodes attaching to existing nodes then need a way to backflow existing information, so that
the entire graph does not need to be re-queried every time a new node attaches

### Node reuse
As mentioned above, many queries in NMA will use the same base views, so there needs to be some sort of method for de-dupping
operators or reusing parts of the graph. This will save on memory usage and peformance (as the same data will not be recalculated many times)

### Active and Static queries
Part of NMA requires static queries: Do we have mod X in loadout Y? Other parts of the app require active queries:
"Let me know whenever a new mod is added to loadout Y". This means that both methods need to be considered
by the framework. Before Cascade was written this required developers in NMA to write queries twice: once with `foreach` or
`linq` and once again using `DynamicData` due to differences in the two approaches this often meant writing the same query
in two completely different ways. A key goal of Cascade is to unify these methods. A query should be written once
and be reusable in both contexts

### Clean interop with existing UI systems
Most of NMA uses Dynamic Data and Rx, these interfaces are fairly well understood, if a bit janky at times, we would like clean
interop with these systems.


## Usage Overview

As mentioned above, Cascade pulls apart the concepts of creation of a dataflow and the context in which that flow is executed.
In other reactive data libraries (like Rx) the context of a flow is liked directly the creation of a flow `observable.Select(...)` ties
the `Select` directly to the specific instance of an observable. It's not possible to define a flow without having a subject
already in existence. This means that the same flow cannot be used in multiple places, it must be re-created for every new observer/subject.

Cascade takes a different approach: Flows are abstract descriptions of how data is processed, flows are then later handed to a
Topology that configures the flow. This is best seen in an example:

```csharp

// Define a inlet, where eventually we will push data into the flow
InletNode<(string Name, int Score)> inlet = new Inlet<(string Name, int Score)>();

// create two flows
Flow<(string Name, int Score)> passingFlow = inlet.Where(x => x.Score > 90);
Flow<(string Name, int Score)> failingFlow = inlet.Where(x => x.Score < 50);

// create an execution topology
var t = new Topology();

OutletNode<(string Name, int Score)> inletNode = t.Intern(inlet);

var passingFlowResults = t.Outlet(passingFlow);
var failingFlowResults = t.Outlet(failingFlow);

// Set the data
inletNode.Values = [("Bill", 40), ("Sally", 99), ("James", 55)];

// The outlets now have the correct data.
passingFlowResults.Should().Be([("Sally", 99)]);
failingFlowResults.Should().Be([("Bill", 40)]);

```

Here we see all the main parts:

* Flow - an abstract description of the transformation of data, can include joins, aggregates, filters and transforms
* Topology - a grouping of nodes that are created by adding flows to the topology
* Inlet - a definition of data that will be injected into the topology
* InletNode - the instance of a given inlet inside a flow
* Outlet - the definition of query results, the "output" of the topology
* OutletNode - a instance of a given outlet in a give Topology

In a full program it is recommended that Inlets, Flows be defined as static members on a class. This
way any part of the application can expand on and reference these primitives. Topologies can then be
created on a per-page, per-module or per-app basis.

## Detailed Overview
Now that the basics of the library are defined, let's go over the design constraints listed above and
discuss how each is handled in Cascade.

### Ad-hoc queries
Since each Topology has a single thread of control, nodes can be attached without concern of the graph updating
while a new subset of the graph is being initialized. Each node in Cascade has a `.Prime()` method that is used
to backflow information into the node. Attaching a new flow to the topology, creates the nodes for the flow first,
then backflows data into the new nodes.

### Node reuse
Since flows are defined at a static level, `ReferenceEquals` can be used to determine if a flow has already
been added to the graph. Each time a new outlet is attached (which requires passing in a flow to attach to the outlet)
the flow it is attached to is compared against the nodes already in the topology, any existing nodes are reused.

### Active and static queries
Since the graph only ever updates when new data is pushed in, and since flows can be added at any time, it is easy
to support both query types by restricting when data updates. This is most clearly seen in MnenmonicDB which automatically
creates a new Topology for every Connection and DB revision. Whenever the database is updated, the topology on the new instance
of IDb is handed the matching DB value, also the new IDb is pushed into the Connection's topology and the old IDb
value is removed. Thanks to some efficient diffing code in MnemonicDB this results in only the changed datoms being pushed
through the Topology.

Back on topic: in MnemonicDB if you want a static query, use `db.Topology` if you want an active query go against
`conn.Topology`.

### Clean interop with existing UI systems
Cascade includes a source generator for `Rows` which are named tuples. On a flow of `Flow<MyRow>` one can call
`.ToActive()` and get back a flow of `Flow<MyRo
w.Active>` which compacts the results together based on a primary
key. These `.Active` rows express their values as a `R3` `BindableReactiveProperty` allowing for binding of data when the
row updates. Outlets themselves are collections and implement `INotifyCollectionChanged` allowing for easy binding to
a outlet in a UI application.


## Complex operators
Beyond the simple `Where` and `Select` operators, Cascade also includes a number of more complex operators. These include:
Joins and Aggregates, both of these work first off the concept of `Rekey`ing.

### Rekeying
Rekeying is the process of tagging a value with a key. For example if one were to join student scores and student ages,
you may wish to key the data based on the student id. Joins take two keyed sources and return a matching of the data based on
keys that match in the two input sets. Aggregates work in a similar way, and that will be discussed later

### Join types
Currently Cascade implements two join types: LeftInnerJoin and LeftOuterJoin. The main difference between the two is what
happens when values are missing from the right side of the Join. LeftInnerJoin will only return values that exist in both
sides of the join, while LeftOuterJoin will return all values from the left side of the join, and any values missing from the
right side will be replaced with a default value.

### Aggregates
One would think that an aggregate is a simple as summing or counting the values in a set. But this is not the case, because most
of the time what people want is a group aggregate. They want to know the max score of students in a clas, not the max score of all
students. Therefore most aggregates require a keying of some sort as input. One could imagine that the max score of students by
class would then look like:

```csharp

Flow<(int ClassId, int MaxScore)> flow = studentScores
    .Rekey(x => x.Class)
    .MaxOf(x => x.Score);
```


## Patterns
So far most of the operators discussed (select, where, join) are fairly low level, in an application like NMA we are often
interested in higher level patterns. Patterns provide a way of abstracting away the details of flows and type creation into
a higher level pattern matching api.

### Simple Pattern Matching
To create a patter, start by calling `Pattern.Create()`, then use `Match` to combine existing flows. Each call to `Match` acts somewhat
like a `Destructure` in C#, in that the values in the flow are pulled apart and joined into the existing pattern. This is best seen
by an example:

```csharp

Flow<(string Name, int Score)> StudentScores =
    Pattern.Create()
       .Define<string>(out var name)
       .Define<int>(out var score)
       .Define<long>(out var id)
       .Match(names, id, name)
       .Match(scores, id, score)
       .Return(name, score);
```

This example shows the basics of pattern, matching, we start by creating the pattern then defining the vars that will
be used in the pattern (called lvars or "logic variables"). Then we call `Match` on the pattern, passing in the vars
that will be used in the match. Each match clause causes a join on the common vars found in the previous flows and
the current flow.

Now clearly, declaring all these variables is a bit of a pain, so Cascade provides overloads for most match methods
that have variants with `out var` baked right into the signature. This allows for a more compact definition of this
pattern matching:

```csharp
Flow<(string Name, int Score)> StudentScores =
    Pattern.Create()
       .Match(names, out var id, out var name)
       .Match(scores, id, out var score)
       .Return(name, score);
```

The pattern matching sytnax is fairly limited by C#'s syntax restrictions, but is type-checked at compile-time. In the
future the system could be improved by using a custom syntax, perhaps patterned after Datalog or Prolog:

```datalog

student_scores(?name, ?score) :-
    names(?id, ?name),
    scores(?id, ?score).

```

### Pattern matching in MnemonicDB

MnemonicDB provides shorthand for using attributes with the pattern matching system. Firstly every attribute is itself a
flow, meaning it can particpate in matching:

```csharp
// Gets the name of the nexus mod for every associated loadout item
Pattern.Create()
   .Match(LibraryLinkedLoadoutItem.LibraryItem, out var loadoutItem, out var libraryItem)
   .Match(NexusModsLibraryItem.ModPage, libraryItem, out var modPage)
   .Match(NexusModsModPageMetadata.Name, modPage, out var name)
   .Return(loadoutItem, name);
```

However, this syntax is a bit strange since the normal way of thinking about tuples in MnemonicDB is `EAV` not `AEV`. So
MnemonicDB provides the `.Db` extension method that allows for a more natural syntax:

```csharp
Pattern.Create()
   .Db(out var loadoutItem, LibraryLinkedLoadoutItem.LibraryItem, out var libraryItem)
   .Db(libraryItem, NexusModsLibraryItem.ModPage, libraryItem)
   .Db(modPage, NexusModsModPageMetadata.Name, out var name)
   .Return(loadoutItem, name);
```

Other methods are supplied in MnemonicDB for doing `LeftOuterJoins` for use with optional attributes or links in the
entity graph that are not always present.

### Pattern matching with Aggregates

Pattern matching can also be used with aggregates, this is done by using one of the aggregation method in the `.Return`
of the pattern, let's take the above example, but modify it to return the number of loadout items for each NexusMod.

```csharp
Pattern.Create()
   .Db(out var loadoutItem, LibraryLinkedLoadoutItem.LibraryItem, out var libraryItem)
   .Db(libraryItem, NexusModsLibraryItem.ModPage, libraryItem)
   .Db(modPage, NexusModsModPageMetadata.Name, out var name)
   .Return(name, loadoutItem.Count());
```

The only difference here is `.Count()` is used in the return. This triggers the pattern compiler to generate the proper
rekeying and aggregation code. Aggregates are fairly easy to add, but initially only `Count`, `Max` and `Sum` are implemented.
