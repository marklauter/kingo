using Kingo.Policies.Pdl;

namespace Kingo.Policies.Tests.Pdl;

public class PdlSerializerTests
{
    [Fact]
    public void Serialize_RoundTrip_ProducesParsableAndEquivalentPdl()
    {
        var originalPdl = File.ReadAllText("Data/doc.policy.pdl");

        _ = PdlParser.Parse(originalPdl)
            .Match(
            Right: doc => _ = PdlParser.Parse(PdlSerializer.Serialize(doc.PolicySet))
                .Match(
                    Right: reparsedDoc => Assert.Equal(doc.PolicySet, reparsedDoc.PolicySet),
                    Left: error => Assert.Fail($"Reparse failed: {error}")
                ),
            Left: error => Assert.Fail($"Initial parse failed: {error}")
        );
    }
}
