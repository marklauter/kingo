# kingo
relationship-based access control (rebac)

## references
- [Datomic Intro](https://www.youtube.com/watch?v=Cym4TZwTCNU)
- [Datomic Information Model](https://www.infoq.com/articles/Datomic-Information-Model/)
- [Google Zanzibar](https://research.google/pubs/zanzibar-googles-consistent-global-authorization-system/)

## policy language
json-based namespace, relation, and rewrite definitions

## policy engine
`check(principal, resource) => traverse(object-relation expression tree) => allowed | denied`

## storage engine
event-based tuple store. account ledger style. current state of a tuple is determined by folding over the tuple's events. periodic snapshots for performance.
example: 
```
event con: t0, tuple:0 (x0, y0)
event mut: t1, tuple:0 x0=x1
event mut: t2, tuple:0 y0=y1

...

read_tuple(0) => fold(tuple:0:events) // yields tuple:0 (x1, y1)
 ```

## dev log
- 20 JUN 2025 - project initiation
- 23 JUN 2025 - created solution
- 23 JUN 2025 - rough in-memory storage engine
- 24 JUN 2025 - began work on simulated key-value store
- 25 JUN 2025 - finished simulated key-value store (DocumentStore)
- 26 JUN 2025 - refactoring primitive types and facts for better domain cohesion
- 27 JUN 2025 - added JSON based namespace specs and subjectset rewrite configuration 
- 30 JUN 2025 - refactored AclStore logic to rewrite rules
- 01 JUL 2025 - planned: refactor AclStore to use DocumentStore
