using Kingo.Policies.Pdl;

namespace Kingo.Policies.Tests.Pdl;

public sealed class PdlParserTests
{
    [Theory]
    [InlineData("policy file rel owner")]
    [InlineData("policy file\nrel owner")]
    [InlineData("policy file\r\nrel owner")]
    [InlineData("policy file rel owner rel editor (dir | cmp owner)")]
    [InlineData("policy file rel owner rel editor (direct | computed owner)")]
    public void Parse_SimpleValidPdl_ReturnsDocument(string pdl) =>
        Assert.True(PdlParser.Parse(pdl).IsRight);
}
