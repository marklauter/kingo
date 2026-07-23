namespace Kingo;

/// <summary>
/// Names the spec revision an evaluation ran under — the value <c>Decision</c> and <c>Expansion</c> carry for replay. Opaque; it arrives
/// as evaluator constructor context beside the injected <c>Spec</c> value.
/// Encoding TBD (content hash, store revision, name plus revision) — stub minted ahead of the storage/versioning design
/// ([[storage-versioning-design]]).
/// </summary>
public sealed record SpecVersion;
