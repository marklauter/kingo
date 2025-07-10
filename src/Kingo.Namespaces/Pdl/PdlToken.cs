using Superpower.Display;

namespace Kingo.Namespaces.Pdl;

public enum PdlToken
{
    None,

    [Token(Category = "identifier", Example = "file")]
    Identifier,

    [Token(Category = "keyword", Example = "ns:")]
    NamespacePrefix,    // ns:

    [Token(Category = "keyword", Example = "re:")]
    RelationshipPrefix, // re:

    [Token(Category = "keyword", Example = "cp:")]
    ComputedPrefix,     // cp:

    [Token(Category = "keyword", Example = "tp:")]
    TuplePrefix,        // tp:

    [Token(Category = "keyword", Example = "this")]
    This,               // this

    [Token(Category = "operator", Example = "|")]
    Union,              // |

    [Token(Category = "operator", Example = "&")]
    Intersection,       // &

    [Token(Category = "operator", Example = "!")]
    Exclusion,          // !

    [Token(Category = "delimiter", Example = "(")]
    LeftParen,          // (

    [Token(Category = "delimiter", Example = ")")]
    RightParen,         // )

    [Token(Category = "delimiter", Example = ",")]
    Comma,              // ,

    [Token(Category = "comment", Example = "# This is a comment")]
    Comment,            // # ...

    [Token(Category = "newline", Example = "\\n")]
    Newline             // \n
}
