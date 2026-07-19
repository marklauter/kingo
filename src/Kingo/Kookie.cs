namespace Kingo;

/// <summary>
/// The Kingo name for Zanzibar's zookie: an opaque value naming a point in the store's timeline. Request-side it is a floor ("no earlier
/// than this snapshot"), consumed at the host edge to pin the fact port; result-side it is the pin ("evaluated at this snapshot"). The
/// pinned port's snapshot token is this type, copied into <c>Decision</c>/<c>Expansion</c> without interpretation.
/// Encoding TBD — stub minted ahead of the storage/versioning design ([[storage-versioning-design]]).
/// </summary>
public sealed record Kookie;
