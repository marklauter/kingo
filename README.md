# kingo
relationship-based access control (rebac)

## opa
https://www.openpolicyagent.org/

## policy language
json-based namespace, relation, and rewrite definitions

## policy engine
`f(tuple) => allowed | denied`

## storage engine
event-based tuple store. account ledger style. current state of a tuple is determined by folding over the tuple's events. periodic snapshots for performance.
example: 
event con: t0, tuple:0 (x0, y0)
event mut: t1, tuple:0 x0=x1
event mut: t2, tuple:0 y0=y1

read_tuple(0) => fold(tuple:0:events) // yields tuple:0 (x1, y1)
 

## dev log
20 JUN 2025 - project initiation
