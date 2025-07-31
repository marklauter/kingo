//using Kingo.Policies.Pdl;
//using LanguageExt;

//namespace Kingo.Policies.Tests;

//public class PdlSerializerTests
//{
//    [Fact]
//    public void Serialize_RoundTrip_ProducesParsableAndEquivalentPdl()
//    {
//        var originalPdl = File.ReadAllText("Data/doc.policy.pdl");

//        _ = PdlParser.Parse(originalPdl).Run().Match(
//            Succ: doc => _ = PdlParser.Parse(PdlSerializer.Serialize(doc.PolicySet)).Run()
//                .Match(
//                    Succ: reparsedDoc => Assert.Equal(doc.PolicySet, reparsedDoc.PolicySet),
//                    Fail: error => Assert.Fail($"Reparse failed: {error}")
//                ),
//            Fail: error => Assert.Fail($"Initial parse failed: {error}")
//        );
//    }
//}
