using Kingo.Storage.Keys;
using LanguageExt;
using System.Numerics;

namespace Kingo.Storage;

public interface ISequence<N>
    where N : INumber<N>
{
    Eff<N> NextAsync(Key name);
}
