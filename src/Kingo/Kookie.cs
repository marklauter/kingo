namespace Kingo;

/// <summary>
/// An opaque value naming a point in the store's timeline. Request-side it is a floor ("no earlier than this snapshot"), consumed at the
/// host edge to pin the fact port. Result-side it is the pin ("evaluated at this snapshot"). The pinned port's snapshot token is this type,
/// copied into <c>Decision</c> and <c>Expansion</c> without interpretation.
/// Encoding is not yet decided: a stub minted ahead of the storage/versioning design ([[storage-versioning-design]]).
/// </summary>
public sealed record Kookie;
