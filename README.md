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
