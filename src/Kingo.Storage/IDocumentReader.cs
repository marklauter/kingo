using System.Numerics;

namespace Kingo.Storage;

public interface IDocumentReader<D>
{
    Task<D?> FindAsync<HK>(HK hashKey, CancellationToken cancellationToken)
        where HK : IEquatable<HK>, IComparable<HK>;

    Task<D?> FindAsync<HK, RK>(HK hashKey, RK rangeKey, CancellationToken cancellationToken)
        where HK : IEquatable<HK>, IComparable<HK>
        where RK : IEquatable<RK>, IComparable<RK>;

    Task<IEnumerable<D>> QueryAsync<HK>(Query<D, HK> query, CancellationToken cancellationToken)
        where HK : IEquatable<HK>, IComparable<HK>;

    Task<IEnumerable<D>> QueryAsync<HK, N>(Query<D, HK, N> query, CancellationToken cancellationToken)
        where HK : IEquatable<HK>, IComparable<HK>
        where N : INumber<N>;
}
