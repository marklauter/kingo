using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "nullable reference types are the contract in the domain and libraries; callers are Kingo's own NRT-clean code. The API host and port projects that face uncontrolled callers guard their edges instead and must not carry this suppression.")]
