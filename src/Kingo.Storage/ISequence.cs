using Kingo.Storage.Keys;
using System.Numerics;

namespace Kingo.Storage;

public interface ISequence<N>
    where N : INumber<N>
{
    Eff<N> NextAsync(Key name);
}
