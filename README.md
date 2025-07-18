# kingo
relationship-based access control (ReBAC) inspired by Google Zanzibar

## inspiration and references
- [Google Zanzibar](https://research.google/pubs/zanzibar-googles-consistent-global-authorization-system/)
- [Datomic Intro](https://www.youtube.com/watch?v=Cym4TZwTCNU)
- [Datomic Information Model](https://www.infoq.com/articles/Datomic-Information-Model/)

## policy specs / subjectset rewrite rules
policy definition language (PDL) for building relationships and rewrite definitions

PDL BNF
```bnf
# operator precedence: !, &, | (exclude, intersect, union)
# expressions
<policy-set>    ::= <namespace> [ <namespace> ]*
<namespace>     ::= <namespace-identifier> <relation-set>
<relation-set>  ::= <relation> [ <relation> ]*
<relation>      ::= <relation-identifier> [ '(' <rewrite> ')' ]
<rewrite>       ::= <intersection> [ '|' <intersection> ]*
<intersection>  ::= <exclusion> [ '&' <exclusion> ]*
<exclusion>     ::= <term> [ '!' <term> ]
<term>          ::= <direct>
                  | <computed-subjectset-rewrite>
                  | <tuple-to-subjectset-rewrite>
                  | '(' <rewrite> ')'

# keywords (terms)
<namespace-identifier>          ::= ('namespace' | '/n') <identifier>
<direct>                        ::= ('direct' | '/d')
<relation-identifier>           ::= ('relation' | '/r') <identifier>
<computed-subjectset-rewrite>   ::= ('computed' | '/c') <identifier>
<tuple-to-subjectset-rewrite>   ::= ('tuple' | '/t') (' <identifier> ',' <identifier> ')'
<identifier>                    ::= [a-zA-Z_][a-zA-Z0-9_]*

<comment>       ::= '#' [^<newline>]*
<newline>       ::= '\n' | '\r\n'
```

PDL sample:
```pdl
# comments are prefixed with #
# rewrite set operators:
#   ! = exclusion operator
#   & = intersection operator
#   | = union operator
# rewrite:
#   directly assigned subjects = direct | dir
#   ComputedSubjectSetRewrite = computed <identifier> | cmp <identifier>
#   TupleToSubjectSetRewrite = tuple (<identifier>, <identifier>) | tpl (<identifier>, <identifier>)

# namespace
namespace file

# empty relationship - implicit direct
relation owner 

# relationship with union rewrite
relation editor (direct | computed owner) 

# relationship with union and exclusion rewrites
relation viewer ((direct | computed editor | tuple (parent, viewer)) ! computed banned) 

# relationship with intersection rewrite
relation auditor (direct & computed viewer) 

# empty relationship - implicit direct
relation banned

# second policy defined within same document
/n folder
/r owner 
/r viewer 
    (
        (/d | /c editor | /t (parent, viewer)) 
        ! /c banned
    )
/r banned
```

## access control subsystem
`is-member(subject, subject-set) => rewrite-expression-tree.traverse() => true | false`
- todo: describe ACL tuples 
- todo: describe ACL tuple binary packing (for now see `performance ideas`)
- todo: describe ACL tuple storage and retrieval
- todo: describe ACL subjectset rewrite recursion 

## storage system
### deprecated
in-memory key-value store with partition key and range key, similar to AWS DocumentDB
### current
simulated key-value store backed by sqlite

each document record is split between two tables. the first table is the header which is composed of a hashkey and version. 
the second table is the journal which is composed of a hashkey, version, and data (dumped as json). the header is mutable and the journal is append only. this means you can maintain an audit for almost zero compute cost. there is no special history table, no need for CDC, and you can read the lastest version without a rollup or slow max calculation on the version.

```ddl
header tbl
PK (text hashkey, number version)
FK (version -> journal:version)

journal tbl
PK (text hashkey, number version)
text data
```

when a document is updated, a new entry is written to journal, and the header version is updated. to read a document you provide the hashkey and the tables are joined like this:
```sql
select
  a.hashkey,
  a.version,
  b.data
from header a
join journal b on b.hashkey = a.hashkey and b.version = a.version
where a.hashkey = @HK
```
 
### future
event-based store like an account ledger. inspired by Datomic. state of an entity is determined by folding over its events. periodic snapshots for performance.

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

key:
```
<date> - work performed
WIP - work in progress
FUT - work planned
```

- 20 JUN 2025 - project initiation
- 23 JUN 2025 - created solution
- 23 JUN 2025 - rough in-memory storage engine
- 24 JUN 2025 - began work on the simulated key-value store
- 25 JUN 2025 - finished simulated key-value store (DocumentStore)
- 26 JUN 2025 - refactoring primitive types and facts for better domain cohesion
- 27 JUN 2025 - added JSON-based namespace specs and subjectset rewrite configuration 
- 30 JUN 2025 - refactored AclStore logic to rewrite rules
- 01 JUL 2025 - refactored AclStore to use DocumentStore
- 02 JUL 2025 - prepped dependencies and document namespaces for refactoring the namespace tree to use the document store
- 02 JUL 2025 - refactored namespace specs to use a document store
- 03 JUL 2025 - tidy before durable storage refactor
- 03 JUL 2025 - began dictionary encoding refactor
- 04 JUL 2025 - began document store refactor - FP: it's turtles all the way down
- 05 JUL 2025 - finished document store refactor
- 06 JUL 2025 - project reorg for better domain cohesion
- 11 JUL 2025 - created policy definition language (PDL)
- 14 JUL 2025 - refactor from Either<Error, Result> to Eff<Result>
- 15 JUL 2025 - SqliteDocumentWriter<HK> complete
- 15 JUL 2025 - added sqlite dbcontext, connection factory, async lock, migrations
- 16 JUL 2025 - deprecated transaction manager
- 16 JUL 2025 - testing showed problems in sequence
- 16 JUL 2025 - minor refactor of PDL
- 16 JUL 2025 - gave up on distributed sequence for now. final attempt was to use twitter snowflake idea, but the value takes 64 bits. for now i'll use sqlite auto-inc PK. this rabbit hole set me back more than a day.
- 16 JUL 2025 - sql document reader is now async. the tx manager is gone (or moved and hidden). the db context is mature. all the sql reader/writer classes now use db context.
- 16 JUL 2025 - abandoned the refactor to distributed sequence with block leases for performance - it was not required
- 16 JUL 2025 - implemented durable storage using SQLite to emulate DynamoDB structure. now support hashkey-value and hashkey:rangekey-value storage. every record is split into two parts. header (composite key + revision) and a journal (composite key + revision + data). header key never changes. header revision is overwritten. journal is append only. journal is a history of changes. header maps to most recent data via the key + version.
- 17 JUL 2025 - woke up understanding distributed sequence and recovered the deleted classes and tests. all tests pass. structure will work with dynamodb.
- WIP - dictionary encoding refactor 

## performance ideas
1. tuples can be packed into the address space of a ulong 
1. something like 64 bits for namespace, resource, and relation, as a partition key and a uint for range key
1. 16 bits for namespace (65k slots)
1. 14 bits for relation (16k slots per namespace)
1. 34 bits for resource (17 billion slots per namespace)
1. 32 bits for users (4 billion)
1. bit packing requires every tuple element to be integer addressable
1. Zanzibar uses a dictionary encoding strategy to map namespaces, relationships, and subjects to integer values
1. the integer values can be packed into that 64-bit mentioned in item 1
1. imagine the tuple lookup as a straight-up integer lookup in a btree or LSMtree - it's fast AF
