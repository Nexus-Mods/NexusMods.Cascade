# Cascade

Cascade is a generic framework for performing either ad-hoc or reactive queries of data.

## Design Overview

At its core this library is a differential dataflow framework. Data is entered into the `Flow` as sets of values. These values are then joined into other sets via various operators (called stages). An application can read these stages to get the final data result, or listen to them via various reactive collection systems. Cascade was designed to work hand-in-hand with MnemonicDB, but is so generic that it likely can be used in many other reactive applications. 

## Differential Dataflow

Dataflow is a rather broad term for a form of computation that operates by having a set of nodes that emit data that is then passed into another set of nodes. Differential dataflow extends on this idea by attaching a delta to each message. So instead of communicating "I saw a cat, I saw another cat, one cat ran away" we would perhaps communicate: `[cat, +2]`, `[cat, -1]`. Differential dataflow is a great optmization for reactive systems because it allows only the parts of the dataflow that care about the deltas to be re-calculated when the deltas change. In the previous example, a query sub-segment that asks "do we have cats" need only re-calculate if the number of cats moves between 0 and 1, if we have 100 cats, it's the same as having one cat (at least in this example, 100 cats in any one location is likely to bring with it a plethora of side effects). 

## Terms

In Cascade, the individual operators in the dataflow are known as "stages" and a group of them is known as a "flow". Each flow is a Directed Acyclic Graph. At least initially the flow executors will not handle cyclic graphs, but there's no technical limitation to adding cycles later on. 

## Usage

The base of a flow is the `InflowStage` these stages do not contain logic of their own, but are updated by code outside the flow. The output of the flow is called a `OutletView`, there are many ways to make these views that expose data in various ways. 

## Flow lock

For performance reasons a given flow is single threaded and carries with it a flow-level "global" lock. This may seem like a serious limitation but it carries with it a plethora of valuable performance benefits. As a former co-worker of mine (halgari speaking here) once said: "My boss has given me one thread, when I can show that I know how to use it properly, he may allow me to have another". There is value to this saying, if enough care is given to optimizing an inner loop, the fact that it is single threaded will matter less than one may expect.

Several of the benefits of a global flow lock are as follows: 

* Inputs to the flow can be in the forms of spans. The `InflowStage` code can lock the flow, use spans to track the input data, then unlock once the processing is done
* Several parts of the flow execution require temporary lists and other collections. These collections can be reset after each execution allowing for re-use of the allocated data. In addition, communication with the logic of each stage can be done via spans instead of via heap objects
* The OutletViews that are bindable via reactiveUI constructs can alert their listeners in one large update batch. Put another way, the graph can execute, then swap to the UI thread and then update *all* the UI at one time.
* Attachment of `OutletViews` to the graph need not carry with them a large amount of failsafe logic for attachment raceconditions. In normal Rx code care must be taken that the initial value from a `.Connect` not skip or double-emit a value. Getting this right is hard to do, and often carries with it a operator level lock
* Stages can be *very* lightweight. Essentially a stage can be a one or more input pointers (pointers to other stages), an update function and a stage Id. The graph updator can keep most of its bookkeeping logic on the stack and not have to create heap lists of "subscribers" and "listeners". 

NOTE: this does not mean that the execution of the graph need always be single-threaded. Only that only one source can be updating the graph at one time. 

