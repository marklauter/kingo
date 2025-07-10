# Kingo
## solution description
The solution, called Kingo, is a Google Zanzibar style ReBAC system.
Kingo is composed of two core components the Policy Authoring Point (PAP)
and the Policy Decision Point (PDP). Users can author namespaces and manage the ACL through the PAP. 
The PDP evaluates namespace rewrite rules against the access control list (ACL) and returns policy decisions.

## key projects
- Kingo: root namespace contains primitive types
- Kingo.Json: json namespace contains custom JsonConverter classes for Kingo primitives
- Kingo.Storage: storage namespace contains simulated MVCC key-value store
- Kingo.Namespaces: namespaces namespace contains PDL namespace classes and PDL parser

PDP decisions are recorded in the descision journal.

Namespaces are defined with a custom policy description language (PDL).

PDL BNF
```bnf
<document> ::= <comment-lines> <namespace-list>

<namespace-list> ::= <namespace>
    | <namespace-list> <comment-lines> <namespace>

<comment-lines> ::= 
    | <comment-lines> <comment> <newline>

<namespace> ::= <namespace-identifier> <newline> <relationship-list>

<relationship-list> ::= <relationship-line>
    | <relationship-list> <relationship-line>

<relationship-line> ::= <relationship> <newline>
    | <comment> <newline>

<relationship> ::= <relationship-identifier>
    | <relationship-identifier> '(' <rewrite-rule> ')'

<rewrite-rule> ::= <all-direct-subjects>
    | <computed-subjectset-rewrite>
    | <tuple-to-subjectset-rewrite>
    | <rewrite-rule> '|' <rewrite-rule>
    | <rewrite-rule> '&' <rewrite-rule>
    | <rewrite-rule> '!' <rewrite-rule>
    | '(' <rewrite-rule> ')'

<all-direct-subjects> ::= 'this'

<computed-subjectset-rewrite> ::= 'cp:' <relationship-name>

<tuple-to-subjectset-rewrite> ::= 'tp:(' <tupleset-relationship> ',' <computed-subjectset-relationship> ')'

<namespace-identifier> ::= 'ns:' <namespace-name>

<relationship-identifier> ::= 're:' <relationship-name>

<tupleset-relationship> ::= <relationship-name>

<computed-subjectset-relationship> ::= <relationship-name>

<namespace-name> ::= <identifier>

<relationship-name> ::= <identifier>

<comment> ::= '#' <text-line>

<newline> ::= '\n'

<text-line> ::= [^\n]*

<identifier> ::= [a-zA-Z_][a-zA-Z0-9_]*
```

sample format:
```pdl
# comments are prefixed with #
# rewrite set operators:
#   | = union operator
#   & = intersection operator
#   ! = exclusion operator
# rewrite rules:
#   directly assigned subjects = this
#   ComputedSubjectSetRewrite = cp:<relationship-name>
#   TupleToSubjectSetRewrite = tp:(<tupleset-relationship>,<computed-subjectset-relationship>)

# namespace name
ns:file

# empty relationship - implicit this
re:owner 

# relationship with union rewrite
re:editor (this | cp:owner) 

# relationship with union and exclusion rewrites
re:viewer ((this | cp:editor | tp:(parent,viewer)) ! cp:banned) 

# relationship with intersection rewrite
re:auditor (this & cp:viewer) 

# empty relationship - implicit this
re:banned 

# second namespace within same document
ns:folder
re:owner 
re:viewer ((this | cp:editor | tp:(parent,viewer)) ! cp:banned) 
```
  
## code standards
 - use dotnet 9
 - use C# 13
 - use LanguageExt 5
 - don't use LanguageExt 4 because it is deprecated
 - use the xUnit testing framework
 - prefer empty collection
 - prefer var
 - prefer immutability
 - prefer empty collection initialization using `[]` for lists and arrays (e.g., `private List<T> list =  - []` instead of `private List<T> list = new List<T>()`) 
 - use 4 spaces for indentation
 - use spaces instead of tabs
 - set tab width to 4 spaces
 - use crlf for new lines
 - insert a final newline at the end of files
 - do not separate import directive groups
 - do not sort system directives first
 - prefer primary constructors
 - never use 'this' for events, fields, methods, and properties
 - prefer using language keywords (eg, int) for locals, parameters, and members
 - prefer using bcl types (eg, int32) for member access
 - prefer not using unnecessary parentheses in arithmetic, relational, and other binary operators
 - prefer not using unnecessary parentheses in other operators
 - require accessibility modifiers for non-interface members
 - prefer using deconstructed variable declarations
 - prefer inlining variable declarations
 - prefer using throw expressions
 - prefer using coalesce expressions
 - prefer using collection initializers
 - prefer using explicit tuple names
 - prefer using null propagation
 - prefer using object initializers
 - prefer auto properties
 - prefer compound assignments
 - prefer conditional expressions over assignments
 - prefer conditional expressions over returns
 - prefer inferred anonymous type member names
 - prefer inferred tuple names
 - prefer is null check over reference equality method
 - mark fields as readonly when possible (warning)
 - prefer var for built-in types, when type is apparent, and elsewhere
 - prefer expression-bodied accessors, constructors, indexers, lambdas, local functions, methods,  - operators, and properties
 - prefer pattern matching over as with null check
 - prefer pattern matching over is with cast check
 - prefer switch expressions
 - prefer conditional delegate calls
 - prefer static local functions
 - follow this modifier order: public, private, protected, internal, static, extern, new, virtual,  - abstract, sealed, override, readonly, unsafe, volatile, async
 - prefer omitting braces for single-line statements
 - strongly prefer simplified using statements (warning)
 - prefer simple default expressions
 - prefer pattern local over anonymous functions
 - prefer index operator
 - prefer range operator
 - prefer discarding unused variables
 - place using directives outside namespace (warning)
 - add new lines before catch, else, finally
 - add new lines before members in anonymous types and object initializers
 - add new lines before open braces
 - add new lines between query expression clauses
 - indent block contents and switch labels
 - don't indent braces or case contents when block
 - indent labels one less than current
 - don't add space after cast
 - add space after colon in inheritance clause
 - add space after comma
 - don't add space after dot
 - add space after keywords in control flow statements
 - add space after semicolon in for statement
 - add space around binary operators
 - don't add space around declaration statements
 - add space before colon in inheritance clause
 - don't add space before comma, dot, or semicolon in for statement
 - don't add space before open square brackets
 - don't add space between empty square brackets
 - don't add space in parameter lists and method calls
 - don't add space between parentheses
 - preserve single line blocks
 - don't preserve single line statements
 - interface names should begin with 'i' (suggestion)
 - type names should use pascalcase (warning)
 - non-field members should use pascalcase (suggestion)
 - async methods should end with 'async' (warning)
 - private or internal fields should use camelcase (suggestion)
 - properties should use pascalcase (warning)
 - public or protected fields should use pascalcase (warning)
 - static fields should use pascalcase (suggestion)
 - private or internal static fields should use pascalcase (suggestion)
 - never use underscore ('_') as a field prefix