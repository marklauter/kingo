using LanguageExt;

namespace Kingo.Facts;

internal static class SubjectExtensions
{
    public static string ToString(this Either<Subject, SubjectSet> user) =>
        user.Match(
            Left: subject => subject.ToString(),
            Right: set => set.ToString());
}

