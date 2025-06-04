# Changelog

## 0.15.0 - 4/6/2025
* Add user defined defaults for `MatchDefault`

## 0.14.0 - 28/5/2025
* Rework how Queries are created, now supports a lazy "no wait" syntax for situations where users will eventually query the data
but cannot block the initial call.

## 0.13.0 - 21/5/2025
* Fix a race condition in ProcessEffects that would result in items being observed twice

## 0.12.0 - 13/5/2025
* Add a "ParallelSelect" operator for parallel processing of values in a select
* Add an async variant of the "Select" operator

## 0.11.0 - 8/5/2025
* Fix `AddRange` support for ObserveCell

## 0.10.0 - 7/5/2025
* Make sure that listeners to queries are initially populated with the current value of the query

## 0.8.0 - 7/5/2025
* Implement `Observe` and `ObserveCell` and rework observables a bit

## 0.7.0 - 5/5/2025
* Renamed "Outlet" to "Query" in most of the user facing code
* Added annotations and interfaces to encourage users to treat queries as disposable resources
* Added disposable interfaces to the topology and query classes

## 0.6.0 - 30/4/2025
* Added support for Union (concat) operator, several other small fixes to source generators

## 0.5.0 - 28/4/2025
* Complete rework of the codebase. First "beta" release.

## 0.1 - 22/1/2025

* Initial release of Cascade
