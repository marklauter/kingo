# kingo
relationship-based access control (rebac) inspired by Google Zanzibar

## references
- [Datomic Intro](https://www.youtube.com/watch?v=Cym4TZwTCNU)
- [Datomic Information Model](https://www.infoq.com/articles/Datomic-Information-Model/)
- [Google Zanzibar](https://research.google/pubs/zanzibar-googles-consistent-global-authorization-system/)

## policy language
json-based namespace, relation, and rewrite definitions

## access control subsystem
`is-member(subject, subject-set) => rewrite-expression-tree.traverse() => allowed | denied`

## storage system
current: in-memory key-value store with partition key and range key, similar to AWS DocumentDB
future: event-based store like an account ledger. inspired by Datomic. current state of an entity is determined by folding over its events. periodic snapshots for performance.
example: 
```
// writes
event con: t0, entity:0 (x0, y0)
event mut: t1, entity:0 x0=x1
event mut: t2, entity:0 y0=y1

// reads
read_tuple(0) => fold(entity:0.events) // yields entity:0 (x1, y1)
 ```

## dev log
- 20 JUN 2025 - project initiation
- 23 JUN 2025 - created solution
- 23 JUN 2025 - rough in-memory storage engine
- 24 JUN 2025 - began work on simulated key-value store
- 25 JUN 2025 - finished simulated key-value store (DocumentStore)
- 26 JUN 2025 - refactoring primitive types and facts for better domain cohesion
- 27 JUN 2025 - added JSON-based namespace specs and subjectset rewrite configuration 
- 30 JUN 2025 - refactored AclStore logic to rewrite rules
- 01 JUL 2025 - refactored AclStore to use DocumentStore
- 02 JUL 2025 - prepped dependencies and document namespaces for refactor namespace tree to use document store
- 02 JUL 2025 - refactored namespace specs to use document store
- 03 JUL 2025 - planned: implement durable storage using SQLite to emulate DynamoDB structure

## performance ideas
1. two million unique tuples can be packed into the address space of a ulong. split 3 ways, 21 bits each ~ 2.1 million addressible tuples
1. bit packing requires every tuple element to be integer addressible
1. Zanzibar uses a dictionary encoding strategy to map namespaces, relationships, subjects to integer values
1. the integer values can be packed into that 64bit mentioned in item 1
1. imagine the tuple lookup as a straight up integer lookup in a btree or LSM - it's fast AF
1. we could encode the resource (the left side of the tuple) with a uint32 and this would act as the partition key on dynamodb. then you could encode the relationship and user (the right side of the tuple) in a packed ulong. This gives you 4 billion addressible relationships
