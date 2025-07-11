# kingo
relationship-based access control (ReBAC) inspired by Google Zanzibar

## inspiration and references
- [Google Zanzibar](https://research.google/pubs/zanzibar-googles-consistent-global-authorization-system/)
- [Datomic Intro](https://www.youtube.com/watch?v=Cym4TZwTCNU)
- [Datomic Information Model](https://www.infoq.com/articles/Datomic-Information-Model/)

## namespace specs / subjectset rewrite rules
json-based namespace, relation, and rewrite definitions

working sample:
```json
{
  "Name": "doc",
  "Relationships": [
    {
      "Name": "owner"
    },
    {
      "Name": "editor",
      "SubjectSetRewrite": {
        "Type": "UnionRewrite",
        "Children": [
          {
            "Type": "This"
          },
          {
            "Type": "ComputedSubjectSetRewrite",
            "Relationship": "owner"
          }
        ]
      }
    },
    {
      "Name": "viewer",
      "SubjectSetRewrite": {
        "Type": "UnionRewrite",
        "Children": [
          {
            "Type": "This"
          },
          {
            "Type": "ComputedSubjectSetRewrite",
            "Relationship": "editor"
          },
          {
            "Type": "TupleToSubjectSetRewrite",
            "TuplesetRelation": "parent",
            "ComputedSubjectSetRelation": "viewer"
          }
        ]
      }
    }
  ]
}
```

custom language (work in progress):
BNF
```xml
<document> ::= <comment-lines> <namespace>

<comment-lines> ::= 
    | <comment-lines> <comment> <newline>

<namespace> ::= <identifier> <newline> <relationship-list>

<relationship-list> ::= <relationship-line>
    | <relationship-list> <relationship-line>

<relationship-line> ::= <relationship> <newline>
    | <comment> <newline>

<relationship> ::= <identifier>
    | <identifier> '(' <rewrite-rule> ')'

<rewrite-rule> ::= 'this'
    | 'cp:' <identifier>
    | 'tp:(' <identifier> ',' <identifier> ')'
    | <rewrite-rule> '|' <rewrite-rule>
    | <rewrite-rule> '&' <rewrite-rule>
    | <rewrite-rule> '!' <rewrite-rule>
    | '(' <rewrite-rule> ')'

<comment> ::= '#' <text-line>

<newline> ::= '\n'
<text-line> ::= [^\n]*
<identifier> ::= [a-zA-Z_][a-zA-Z0-9_]*
```

sample format:
```csharp
# comments are prefixed with #
# <namespace>
# <relationship>(<rewrite-rule>)
# rewrite set operators:
# | = union operator
# & = intersection operator
# ! = exclusion operator
# rewrite rules:
# directly assigned subjects = this
# ComputedSubjectSetRewrite = cp:<relationship>
# TupleToSubjectSetRewrite = tp:(<tupleset-relationship>,<computed-subjectset-relationship>)

# namespace name
file

# empty relationship - implicit this
owner 

# relationship with union rewrite
editor (this | cp:owner) 

# relationship with union and exclusion rewrites
viewer ((this | cp:editor | tp:(parent,viewer)) ! cp:banned) 

# relationship with intersection rewrite
auditor (this & cp:viewer) 

# empty relationship - implicit this
banned 
```

## access control subsystem
`is-member(subject, subject-set) => rewrite-expression-tree.traverse() => true | false`
- todo: describe ACL tuples 
- todo: describe ACL tuple binary packing (for now, see `performance ideas`)
- todo: describe ACL tuple storage and retrieval
- todo: describe ACL subjectset rewrite recursion 

## storage system
- current: in-memory key-value store with partition key and range key, similar to AWS DocumentDB
- future: an event-based store like an account ledger. inspired by Datomic. entity state is determined by folding over its events. periodic snapshots for performance.

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
- WIP - project reorg for better domain cohesion
- WIP - namespace specification language
- WIP - dictionary encoding refactor
- FUT: Implement durable storage using SQLite to emulate DynamoDB structure

## performance ideas
1. tuples can be packed into the address space of a ulong 
1. something like 64 bits for namespace, resource, and relation, as a partition key, and a uint for range key
1. 16 bits for namespace (65k slots)
1. 14 bits for relation (16k slots per namespace)
1. 34 bits for resource (17 billion slots per namespace)
1. 32 bits for users (4 billion)
1. bit packing requires every tuple element to be integer addressable
1. Zanzibar uses a dictionary encoding strategy to map namespaces, relationships, and subjects to integer values
1. the integer values can be packed into the 64-bit mentioned in item 1
1. imagine the tuple lookup as a straight-up integer lookup in a btree or LSM - it's fast AF
