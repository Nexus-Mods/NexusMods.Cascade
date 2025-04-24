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


