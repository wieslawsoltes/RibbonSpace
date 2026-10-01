using RibbonSpace.Controls;

// XAML namespaces of RibbonSpace (Uno.Sdk implicit XAML namespaces, on by default):
// - the global namespace: RibbonSpace types work without any prefix or declaration (<Ribbon>, <RibbonTitleBar>);
// - one XML namespace URI with the default prefix `rs` (<rs:Ribbon>), available without declaration too, for
//   disambiguation or when the global namespace is turned off.
// WinUI's own XAML compiler (Windows App SDK heads) supports neither yet, so XAML compiled for Windows App SDK keeps
// `xmlns:rs="using:RibbonSpace.Controls"`. The attributes come from Uno.Xaml (System.Windows.Markup), which every Uno
// runtime target references.
#if !WINDOWS
[assembly: System.Windows.Markup.XmlnsDefinition(RibbonXmlns.GlobalUri, "RibbonSpace.Controls")]
[assembly: System.Windows.Markup.XmlnsDefinition(RibbonXmlns.GlobalUri, "RibbonSpace.Controls.Primitives")]
[assembly: System.Windows.Markup.XmlnsDefinition(RibbonXmlns.GlobalUri, "RibbonSpace.Controls.Mvvm")]
[assembly: System.Windows.Markup.XmlnsDefinition(RibbonXmlns.Uri, "RibbonSpace.Controls")]
[assembly: System.Windows.Markup.XmlnsDefinition(RibbonXmlns.Uri, "RibbonSpace.Controls.Primitives")]
[assembly: System.Windows.Markup.XmlnsDefinition(RibbonXmlns.Uri, "RibbonSpace.Controls.Mvvm")]
[assembly: System.Windows.Markup.XmlnsPrefix(RibbonXmlns.Uri, RibbonXmlns.Prefix)]
#endif

namespace RibbonSpace.Controls
{
    /// <summary>XML namespace of the RibbonSpace XAML types.</summary>
    public static class RibbonXmlns
    {
        /// <summary>
        /// XML namespace URI mapping <c>RibbonSpace.Controls</c>, <c>RibbonSpace.Controls.Primitives</c> and
        /// <c>RibbonSpace.Controls.Mvvm</c>: <c>xmlns:rs="https://github.com/wieslawsoltes/RibbonSpace"</c>.
        /// </summary>
        public const string Uri = "https://github.com/wieslawsoltes/RibbonSpace";

        /// <summary>Default prefix, available without declaration when implicit XAML namespaces are enabled.</summary>
        public const string Prefix = "rs";

        /// <summary>
        /// Uno's global XAML namespace (default of the <c>UnoGlobalXamlNamespaceUri</c> MSBuild property). RibbonSpace
        /// registers its namespaces there, so its types resolve without a prefix.
        /// </summary>
        public const string GlobalUri = "http://schemas.microsoft.com/winfx/2006/xaml/presentation/global";
    }
}
