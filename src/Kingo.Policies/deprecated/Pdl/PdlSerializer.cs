//using System.Diagnostics.CodeAnalysis;
//using System.Text;

//namespace Kingo.Policies.Pdl;

///// <summary>
///// PDL BNF
///// # operator precedence: !, &, | (exclude, intersect, union)
///// # expressions
///// <policy-set>    ::= <policy> [ <policy> ]*
///// <policy>        ::= <policy-identifier> <relation-set>
///// <relation-set>  ::= <relation> [ <relation> ]*
///// <relation>      ::= <relation-identifier> [ '(' <rewrite> ')' ]
///// <rewrite>       ::= <intersection> [ '|' <intersection> ]*
///// <intersection>  ::= <exclusion> [ '&' <exclusion> ]*
///// <exclusion>     ::= <term> [ '!' <term> ]
///// <term>          ::= <direct>
/////                   | <computed-subjectset-rewrite>
/////                   | <tuple-to-subjectset-rewrite>
/////                   | '(' <rewrite> ')'
///// 
///// # keywords (terms)
///// <policy-identifier>             ::= 'policy' <identifier>
///// <direct>                        ::= ('direct' | 'dir')
///// <relation-identifier>           ::= ('relation' | 'rel')  <identifier>
///// <computed-subjectset-rewrite>   ::= ('computed' | 'cmp') <identifier>
///// <tuple-to-subjectset-rewrite>   ::= ('tuple' | 'tpl') (' <identifier> ',' <identifier> ')'
///// <identifier>                    ::= [a-zA-Z_][a-zA-Z0-9_]*
///// 
///// <comment>       ::= '#' [^<newline>]*
///// <newline>       ::= '\n' | '\r\n'
///// </summary>
//public static class PdlSerializer
//{
//    [SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "it's fine")]
//    public static string Serialize(NamespaceSet policySet) =>
//        new StringBuilder()
//            .AppendLine("# pdl version: 1.0.0")
//            .AppendLine($"# decompiled from AST: {DateTime.UtcNow:O}")
//            .Append(string.Join("\r\n\r\n", policySet.Policies.Map(Serialize)))
//            .AppendLine()
//            .ToString();

//    private static string Serialize(Namespace policy) =>
//        $"namespace {policy.Name}\r\n{string.Join("\r\n", policy.Relations.Map(Serialize))}";

//    private static string Serialize(Relation relation) =>
//        relation.SubjectSetRewrite is DirectRewrite
//            ? $"relation {relation.Name}"
//            : $"relation {relation.Name} ({Serialize(relation.SubjectSetRewrite, false)})";

//    private static string Serialize(SubjectSetRewrite rewrite, bool parenthesize)
//    {
//        var content = rewrite switch
//        {
//            DirectRewrite => "direct",
//            ComputedSubjectSetRewrite c => $"computed {c.Relation}",
//            TupleToSubjectSetRewrite t => $"tuple ({t.TuplesetRelation}, {t.ComputedSubjectSetRelation})",
//            UnionRewrite u => string.Join(" | ", u.Children.Map(child => Serialize(child, IsComplex(child)))),
//            IntersectionRewrite i => string.Join(" & ", i.Children.Map(child => Serialize(child, IsComplex(child)))),
//            ExclusionRewrite e => $"{Serialize(e.Include, IsComplex(e.Include))} ! {Serialize(e.Exclude, IsComplex(e.Exclude))}",
//            _ => string.Empty
//        };
//        return parenthesize ? $"({content})" : content;
//    }

//    private static bool IsComplex(SubjectSetRewrite rewrite) =>
//        rewrite is not (DirectRewrite or ComputedSubjectSetRewrite or TupleToSubjectSetRewrite);
//}
