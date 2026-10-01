using RibbonSpace.Controls;
using RibbonSpace.Uno.RuntimeTests.Xaml;

namespace RibbonSpace.Uno.RuntimeTests;

/// <summary>The RibbonSpace XML namespace URI and the implicit <c>rs</c> prefix resolve in XAML.</summary>
public sealed class XamlNamespaceTests : RuntimeTestBase
{
    [RibbonTest]
    public async Task Declared_xml_namespace_uri_resolves_types()
    {
        var view = await Mount(new ExplicitXmlnsView(), 1200);
        Assert.True(view.RibbonElement.FindItem("cut") is RibbonButton, "Ribbon content created from the URI namespace");
        Assert.Equal("https://github.com/wieslawsoltes/RibbonSpace", RibbonXmlns.Uri);
    }

    [RibbonTest]
    public async Task Implicit_rs_prefix_resolves_types_without_declaration()
    {
        var view = await Mount(new ImplicitXmlnsView(), 1200);
        Assert.True(view.RibbonElement.FindItem("bold") is RibbonToggleButton, "Ribbon content created with the implicit prefix");
        Assert.NotNull(view.IconElement, "Primitives types resolve through the same prefix");
    }

    [RibbonTest]
    public async Task Unprefixed_types_resolve_through_the_global_namespace()
    {
        var view = await Mount(new GlobalXmlnsView(), 1200);
        var find = view.RibbonElement.FindItem("find") as RibbonButton;
        Assert.True(find is not null, "Unprefixed elements and property elements resolve");
        Assert.Equal(80d, find!.MinWidth, "Style TargetType resolves with the implicit rs prefix");
        Assert.Equal("FD", RibbonKeyTip.GetKeyTip(find), "Unprefixed attached properties resolve");
        Assert.Equal("Document", view.TitleBarElement.Title);
        Assert.Equal("save", string.Join(",", view.RibbonElement.GetQuickAccessItemIds()));
        Assert.NotNull(view.IconElement, "Primitives types resolve without a prefix");
    }
}
