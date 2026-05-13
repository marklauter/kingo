# Catch-all exception handler in PdlParser.Parse wraps unrelated failures

Severity: important
Type: code
Location: `Kingo.Pdl/PdlParser.cs:11`
Principle: Fail loud when prevention fails
catch (Exception) wraps OutOfMemoryException, OperationCanceledException, and other infrastructure failures as PdlParseException, hiding their type from callers.

## Observation
The catch block at `PdlParser.Parse` line 24 reads:
```csharp
catch (Exception ex) when (ex is not PdlParseException)
{
    throw new PdlParseException(PdlParseErrorCodes.SyntaxError, $"failed to parse PDL document: {ex.Message}", ex);
}
```
Every exception type except `PdlParseException` is converted to `PdlParseException` with code `SyntaxError`. That includes `OperationCanceledException`, `OutOfMemoryException`, `ThreadAbortException`, and any future exception YamlDotNet or its dependencies might add.

## Why it matters
writing-csharp: "No catch-all handlers. Catch specific exception types you can act on; let everything else propagate."

A caller who wants to handle cancellation will see `PdlParseException` and assume the input was bad. A caller running under memory pressure cannot distinguish a malformed YAML document from process distress. The wrapper also mislabels every non-syntax failure as `PdlParseErrorCodes.SyntaxError`.

## Suggested fix
Catch the specific exception type(s) YamlDotNet raises for malformed input — `YamlDotNet.Core.YamlException` and `YamlDotNet.Core.SyntaxErrorException` — and let everything else propagate:

```csharp
catch (YamlException ex)
{
    throw new PdlParseException(PdlParseErrorCodes.SyntaxError, $"failed to parse PDL document: {ex.Message}", ex);
}
```

If a known set of non-Yaml exceptions can legitimately escape the deserializer (e.g., the type converters throwing `ArgumentException` on bad identifiers), add a second `catch` for that specific type. Cancellation and resource-pressure exceptions stay unwrapped.
