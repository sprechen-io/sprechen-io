namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class SchemaNodeDefaultPropertySelectorTests
{
    private readonly SchemaNodeDefaultPropertySelector _selector = new();

    [Fact]
    public void ShouldInitialize_OnlyRequiredConstOrDefaultProperties()
    {
        Assert.True(_selector.ShouldInitialize(new SchemaNode { Name = "required", Type = SchemaNodeType.String, IsRequired = true }));
        Assert.True(_selector.ShouldInitialize(new SchemaNode { Name = "const", Type = SchemaNodeType.String, ConstValue = "fixed" }));
        Assert.True(_selector.ShouldInitialize(new SchemaNode { Name = "default", Type = SchemaNodeType.String, DefaultValue = "value" }));
        Assert.False(_selector.ShouldInitialize(new SchemaNode { Name = "optional", Type = SchemaNodeType.String }));
    }
}
