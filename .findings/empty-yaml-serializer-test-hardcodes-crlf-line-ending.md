# Empty YAML serializer test hardcodes CRLF line ending

Severity: nit
Type: code
Location: `Kingo.Pdl.Tests/PdlSerializerTests.cs:97`
Principle: One source of truth
Assert.Equal hardcodes "{}\r\n" which may not match YamlDotNet output on Linux CI runners.

## Observation
```csharp
[Fact]
public void Serialize_EmptyDocument_ReturnsEmptyYaml()
{
    var document = new PdlDocument("original", []);
    var yaml = PdlSerializer.Serialize(document);
    Assert.Equal("{}\r\n", yaml);
}
```
The expected value is `"{}\r\n"`. The CI workflow (`.github/workflows/dotnet.tests.yml`) runs on `ubuntu-latest`. YamlDotNet defaults to writing `Environment.NewLine`, which is `\n` on Linux.

## Why it matters
The test will pass locally on Windows and fail in CI. The assertion is fragile against the platform's line-ending convention rather than the serializer's behaviour.

## Suggested fix
Assert on shape instead of byte-exact equality:

```csharp
Assert.Equal("{}", yaml.TrimEnd('\r', '\n'));
```

Or normalize before comparing:

```csharp
Assert.Equal("{}\n", yaml.Replace("\r\n", "\n"));
```

A second option: pin the line ending in `SerializerBuilder` (`.WithNewLine("\n")`) and assert against that — but that's a behaviour change to the serializer, not just the test.
