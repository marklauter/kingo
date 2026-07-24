namespace Kingo;

/// <summary>
/// The spec revision an evaluation ran under, the value <c>Decision</c> and <c>Expansion</c> carry for replay. Opaque. It arrives
/// as evaluator constructor context beside the injected <c>Domain</c> value.
/// Encoding is not yet decided: content hash, store revision, or name plus revision. A stub minted ahead of the storage/versioning design
/// ([[storage-versioning-design]]).
/// </summary>
public sealed record DomainVersion;
