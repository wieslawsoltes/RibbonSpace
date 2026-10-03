// MSBuild task (RoslynCodeTaskFactory) of build/WinUI/RibbonSpace.WinUI.targets. The WinUI projects link the
// RibbonSpace.Uno sources; this task adapts their XAML to what the WinUI XAML compiler accepts:
// - Uno resolves RibbonSpace types without a prefix (global XAML namespace), through the XML namespace URI
//   (https://github.com/wieslawsoltes/RibbonSpace) and through the undeclared `rs` prefix (XmlnsPrefix). WinUI only
//   knows `using:` namespaces, so those elements, property elements and attached properties move to
//   `using:RibbonSpace.Controls[.Primitives|.Mvvm]`.
// - Resource URIs carry the assembly name (ms-appx:///RibbonSpace.Uno/...), which differs per head.
// Adapted copies go to the intermediate directory and replace the Page / ApplicationDefinition items; files that
// need no change are copied verbatim.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public sealed class RibbonSpaceAdaptXaml : Task
{
    private const string RibbonUri = "https://github.com/wieslawsoltes/RibbonSpace";
    private const string GlobalUri = "http://schemas.microsoft.com/winfx/2006/xaml/presentation/global";
    private const string PresentationUri = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private const string ImplicitPrefix = "rs";

    private static readonly Dictionary<string, string> Prefixes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["RibbonSpace.Controls"] = "rs",
        ["RibbonSpace.Controls.Primitives"] = "rsp",
        ["RibbonSpace.Controls.Mvvm"] = "rsm",
    };

    private static readonly Regex NamespacePattern = new Regex(@"^\s*namespace\s+([\w.]+)", RegexOptions.Multiline);
    private static readonly Regex TypePattern = new Regex(@"\bpublic\s+(?:(?:static|sealed|abstract|partial|readonly|unsafe|ref)\s+)*(?:class|struct|enum|interface|record)\s+(\w+)");
    private static readonly Regex ImplicitPrefixUse = new Regex(@"[<\s""'{]" + ImplicitPrefix + @":[A-Za-z_]");
    private static readonly Regex ImplicitPrefixDeclaration = new Regex(@"\sxmlns:" + ImplicitPrefix + @"\s*=");
    private static readonly Regex RootStartTag = new Regex(@"<(?![?!])[\w:.]+");

    /// <summary>XAML items to adapt.</summary>
    [Required]
    public ITaskItem[] Items { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>C# sources declaring the XAML-visible RibbonSpace types.</summary>
    [Required]
    public ITaskItem[] TypeSources { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>Directory receiving the adapted files (under their Link path).</summary>
    [Required]
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>Resource URI prefix to replace, e.g. <c>ms-appx:///RibbonSpace.Uno/</c>.</summary>
    public string ResourceRootFrom { get; set; } = string.Empty;

    /// <summary>Replacement of <see cref="ResourceRootFrom"/>, e.g. <c>ms-appx:///RibbonSpace.WinUI/</c>.</summary>
    public string ResourceRootTo { get; set; } = string.Empty;

    /// <summary>The adapted items, with the metadata of the originals.</summary>
    [Output]
    public ITaskItem[] AdaptedItems { get; set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
        var types = ReadTypes();
        var adapted = new List<ITaskItem>();
        foreach (var item in Items)
        {
            var source = item.GetMetadata("FullPath");
            var link = item.GetMetadata("Link");
            if (string.IsNullOrEmpty(link))
            {
                link = Path.IsPathRooted(item.ItemSpec) || item.ItemSpec.StartsWith("..", StringComparison.Ordinal) ? Path.GetFileName(source) : item.ItemSpec;
            }

            string text;
            try
            {
                text = Adapt(File.ReadAllText(source), types);
            }
            catch (XmlException ex)
            {
                Log.LogError(null, "RS0001", null, source, ex.LineNumber, ex.LinePosition, 0, 0, "RibbonSpace could not adapt the XAML for WinUI: " + ex.Message);
                continue;
            }

            var target = Path.GetFullPath(Path.Combine(OutputDirectory, link));
            WriteIfChanged(target, text);
            var output = new TaskItem(target);
            item.CopyMetadataTo(output);
            output.SetMetadata("Link", link);
            output.SetMetadata("RibbonSpaceSource", source);
            adapted.Add(output);
        }

        AdaptedItems = adapted.ToArray();
        return !Log.HasLoggedErrors;
    }

    private Dictionary<string, string> ReadTypes()
    {
        var types = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var item in TypeSources)
        {
            var code = File.ReadAllText(item.GetMetadata("FullPath"));
            var ns = NamespacePattern.Match(code);
            if (!ns.Success || !Prefixes.ContainsKey(ns.Groups[1].Value))
            {
                continue;
            }

            foreach (Match type in TypePattern.Matches(code))
            {
                if (!types.ContainsKey(type.Groups[1].Value))
                {
                    types.Add(type.Groups[1].Value, ns.Groups[1].Value);
                }
            }
        }

        return types;
    }

    private string Adapt(string original, IReadOnlyDictionary<string, string> types)
    {
        var text = original;
        if (!string.IsNullOrEmpty(ResourceRootFrom))
        {
            text = text.Replace(ResourceRootFrom, ResourceRootTo);
        }

        var changed = text != original;
        if (ImplicitPrefixUse.IsMatch(text) && !ImplicitPrefixDeclaration.IsMatch(text))
        {
            // Uno's XmlnsPrefix makes `rs:` usable without a declaration.
            var root = RootStartTag.Match(text);
            if (root.Success)
            {
                text = text.Insert(root.Index + root.Length, " xmlns:" + ImplicitPrefix + "=\"" + RibbonUri + "\"");
                changed = true;
            }
        }

        var document = XDocument.Parse(text, LoadOptions.PreserveWhitespace);
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var element in document.Descendants().ToList())
        {
            var ns = element.Name.NamespaceName;
            if (ns == PresentationUri || ns == GlobalUri || ns == RibbonUri)
            {
                // Property elements (<Ribbon.Tabs>) take the namespace of their owner type.
                var owner = element.Name.LocalName.Split('.')[0];
                if (!types.TryGetValue(owner, out var clr) && ns == RibbonUri)
                {
                    clr = "RibbonSpace.Controls";
                }

                if (clr is not null)
                {
                    element.Name = XNamespace.Get("using:" + clr) + element.Name.LocalName;
                    used.Add(clr);
                    changed = true;
                }
                else if (ns == GlobalUri)
                {
                    element.Name = XNamespace.Get(PresentationUri) + element.Name.LocalName;
                    changed = true;
                }
            }

            foreach (var attribute in element.Attributes().ToList())
            {
                var attributeNs = attribute.Name.NamespaceName;
                var dot = attribute.Name.LocalName.IndexOf('.');
                if (attribute.IsNamespaceDeclaration || dot <= 0 || (attributeNs.Length > 0 && attributeNs != GlobalUri && attributeNs != RibbonUri))
                {
                    continue;
                }

                // Attached properties: RibbonKeyTip.KeyTip="..." -> rs:RibbonKeyTip.KeyTip="...".
                if (types.TryGetValue(attribute.Name.LocalName.Substring(0, dot), out var owner))
                {
                    attribute.Remove();
                    element.SetAttributeValue(XNamespace.Get("using:" + owner) + attribute.Name.LocalName, attribute.Value);
                    used.Add(owner);
                    changed = true;
                }
            }
        }

        if (!changed)
        {
            return original;
        }

        // Declarations of the URI namespaces now point at the main CLR namespace, so prefixed type names inside
        // attribute values (TargetType="rs:RibbonButton") keep resolving.
        foreach (var declaration in document.Descendants().Attributes().Where(a => a.IsNamespaceDeclaration && (a.Value == RibbonUri || a.Value == GlobalUri)).ToList())
        {
            declaration.Value = declaration.Value == GlobalUri && declaration.Name.LocalName == "xmlns" ? PresentationUri : "using:RibbonSpace.Controls";
        }

        // Declare every used namespace on the root, otherwise LINQ to XML emits p1, p2, ... per element.
        var rootElement = document.Root!;
        foreach (var clr in used.OrderBy(c => c, StringComparer.Ordinal))
        {
            var uri = "using:" + clr;
            if (rootElement.GetPrefixOfNamespace(uri) is not null)
            {
                continue;
            }

            var prefix = Prefixes[clr];
            var candidate = prefix;
            for (var i = 2; rootElement.GetNamespaceOfPrefix(candidate) is not null; i++)
            {
                candidate = prefix + i;
            }

            rootElement.SetAttributeValue(XNamespace.Xmlns + candidate, uri);
        }

        var settings = new XmlWriterSettings { OmitXmlDeclaration = true, NewLineHandling = NewLineHandling.Entitize };
        var builder = new StringBuilder();
        using (var writer = XmlWriter.Create(builder, settings))
        {
            document.Save(writer);
        }

        return builder.ToString();
    }

    private static void WriteIfChanged(string path, string text)
    {
        // Keep timestamps of unchanged files so the XAML compiler stays incremental.
        if (File.Exists(path) && File.ReadAllText(path) == text)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text, new UTF8Encoding(false));
    }
}
