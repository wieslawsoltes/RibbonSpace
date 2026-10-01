using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RibbonSpace.Controls;

namespace RibbonSpace.Uno.RuntimeTests;

/// <summary>Builds a Word-like ribbon in code for tests.</summary>
public static class SampleRibbon
{
    public static Ribbon Create()
    {
        var ribbon = new Ribbon();
        var home = new RibbonTab { Id = "home", Header = "Home" };
        var clipboard = new RibbonGroup { Id = "clipboard", Header = "Clipboard", IsDialogLauncherVisible = true };
        clipboard.Items.Add(new RibbonSplitButton { Id = "paste", Label = "Paste", Icon = "", SizeDefinition = "Large", Flyout = Menu("Keep Source", "Text Only") });
        clipboard.Items.Add(new RibbonButton { Id = "cut", Label = "Cut", Icon = "", Shortcut = "Ctrl+X" });
        clipboard.Items.Add(new RibbonButton { Id = "copy", Label = "Copy", Icon = "", CommandId = "copy" });
        clipboard.Items.Add(new RibbonToggleButton { Id = "painter", Label = "Format Painter", Icon = "", Size = RibbonItemSize.Medium });
        home.Groups.Add(clipboard);

        var font = new RibbonGroup { Id = "font", Header = "Font", ItemsLayout = RibbonGroupItemsLayout.Rows, RowCount = 2 };
        var row1 = new RibbonStackPanel();
        row1.Items.Add(new RibbonFontComboBox { Id = "fontFamily", Text = "Segoe UI" });
        row1.Items.Add(new RibbonFontSizeComboBox { Id = "fontSize", Text = "11" });
        var row2 = new RibbonStackPanel();
        var styles = new RibbonButtonGroup();
        styles.Items.Add(new RibbonToggleButton { Id = "bold", Label = "Bold", Icon = "", Shortcut = "Ctrl+B", KeyTip = "1" });
        styles.Items.Add(new RibbonToggleButton { Id = "italic", Label = "Italic", Icon = "", KeyTip = "2" });
        row2.Items.Add(styles);
        row2.Items.Add(new RibbonColorPicker { Id = "fontColor", Label = "Font Color" });
        font.Items.Add(row1);
        font.Items.Add(row2);
        home.Groups.Add(font);

        var paragraph = new RibbonGroup { Id = "paragraph", Header = "Paragraph" };
        foreach (var (id, label, glyph) in new[] { ("left", "Align Left", ""), ("center", "Center", ""), ("right", "Align Right", "") })
        {
            paragraph.Items.Add(new RibbonToggleButton { Id = id, Label = label, Icon = glyph, GroupName = "align", Size = RibbonItemSize.Medium, IsChecked = id == "left" });
        }

        home.Groups.Add(paragraph);
        var gallery = new RibbonGroup { Id = "styles", Header = "Styles", ReductionOrder = 1 };
        var g = new RibbonGallery { Id = "stylesGallery", Label = "Styles", MaxColumns = 5, MinColumns = 2 };
        foreach (var name in new[] { "Normal", "No Spacing", "Heading 1", "Heading 2", "Title", "Subtitle", "Quote" })
        {
            g.Items.Add(new RibbonGalleryItem { Label = name, Value = name, PreviewText = "AaBb", Category = name.StartsWith("Heading", StringComparison.Ordinal) ? "Headings" : "Paragraph" });
        }

        gallery.Items.Add(g);
        home.Groups.Add(gallery);
        var editing = new RibbonGroup { Id = "editing", Header = "Editing" };
        editing.Items.Add(new RibbonButton { Id = "find", Label = "Find", Icon = "", Size = RibbonItemSize.Large });
        editing.Items.Add(new RibbonButton { Id = "replace", Label = "Replace", Icon = "", Size = RibbonItemSize.Large });
        home.Groups.Add(editing);
        ribbon.Tabs.Add(home);

        var insert = new RibbonTab { Id = "insert", Header = "Insert" };
        var tables = new RibbonGroup { Id = "tables", Header = "Tables" };
        tables.Items.Add(new RibbonButton { Id = "table", Label = "Table", Icon = "", Size = RibbonItemSize.Large });
        insert.Groups.Add(tables);
        ribbon.Tabs.Add(insert);

        ribbon.ContextualGroups.Add(new RibbonContextualTabGroup { Id = "tableTools", Header = "Table Tools", Activation = RibbonContextualActivation.SelectOnShow });
        var design = new RibbonTab { Id = "tableDesign", Header = "Table Design", ContextualGroupId = "tableTools" };
        var options = new RibbonGroup { Id = "tableOptions", Header = "Options" };
        options.Items.Add(new RibbonCheckBox { Id = "headerRow", Label = "Header Row", IsChecked = true });
        design.Groups.Add(options);
        ribbon.Tabs.Add(design);

        ribbon.QuickAccessToolBar!.Items.Add(new RibbonButton { Id = "save", Label = "Save", Icon = "" });
        ribbon.Backstage = new RibbonBackstage { Title = "Tests" };
        ribbon.Backstage.Items.Add(new RibbonBackstageItem { Id = "info", Header = "Info", Content = new TextBlock { Text = "Info page" } });
        ribbon.Backstage.Items.Add(new RibbonBackstageItem { Id = "saveAction", Header = "Save" });
        return ribbon;
    }

    public static MenuFlyout Menu(params string[] items)
    {
        var menu = new MenuFlyout();
        foreach (var item in items)
        {
            menu.Items.Add(new MenuFlyoutItem { Text = item });
        }

        return menu;
    }

    public static T Item<T>(this Ribbon ribbon, string id)
        where T : FrameworkElement
        => (T)(ribbon.FindItem(id) ?? throw new InvalidOperationException($"Item '{id}' not found"));

    public static RibbonGroup Group(this Ribbon ribbon, string id)
        => ribbon.Tabs.SelectMany(t => t.Groups).First(g => g.Id == id);
}
