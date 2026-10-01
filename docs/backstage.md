# Backstage (File view)

```xml
<rs:Ribbon.Backstage>
  <rs:RibbonBackstage Title="Word" NavigationPaneWidth="220">
    <rs:RibbonBackstageItem Id="home" Header="Home" Icon="&#xE80F;" KeyTip="H">
      <local:BackstageHomePage />                           <!-- page content -->
    </rs:RibbonBackstageItem>
    <rs:RibbonBackstageItem Id="save" Header="Save" Icon="&#xE74E;" Command="{x:Bind ViewModel.SaveCommand}" />  <!-- action -->
    <rs:RibbonBackstageItem Id="info" Header="Info" HasSeparatorBefore="True" Content="{x:Bind ViewModel.Info}" ContentTemplate="{StaticResource InfoTemplate}" />
    <rs:RibbonBackstageItem Id="options" Header="Options" Icon="&#xE713;" Placement="Bottom" />
  </rs:RibbonBackstage>
</rs:Ribbon.Backstage>
```

- **Open / close:**
  - the File button, `IsBackstageOpen`, `InvokeApplicationButton()`
  - the back button, Esc, or action items with `ClosesBackstage`
  - `BackstageOpened` / `BackstageClosed` events
- **Items:**
  - Page items (with `Content` / `ContentTemplate`) are selectable.
  - Action items (with `Command`) execute and close the backstage.
  - `Placement="Bottom"` puts items in the footer (Account, Feedback, Options).
- **Presentation:**
  - an accent navigation pane
  - a page title (`ShowPageTitle`)
  - `PaneHeader` content
  - a slide-in animation
  - full-window popup sized to the window
  - light and dark themes
- **KeyTips:** once the backstage is open, KeyTips cover the back button and the navigation items.
- **Search:** backstage items appear in command search.
- **Application menu instead of a backstage:** leave `Backstage` null and set `ApplicationMenu` (any `FlyoutBase`),
  as the CadSpace application menu does. Alternatively, handle `ApplicationButtonClick` and set `Handled = true`.
- **MVVM:** `RibbonModel.Backstage` (`RibbonBackstageModel`: `Items`, `SelectedItem`, `IsOpen`, `Title`).
