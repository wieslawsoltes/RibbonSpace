# Quick Access Toolbar

```xml
<rs:Ribbon QuickAccessPosition="AboveRibbon" IsQuickAccessVisible="True" ShowQuickAccessLabels="False">
  <rs:Ribbon.QuickAccessToolBar>
    <rs:RibbonQuickAccessToolBar>
      <rs:RibbonButton Id="save" Label="Save" Icon="&#xE74E;" Shortcut="Ctrl+S" />
      <rs:RibbonButton Id="undo" Label="Undo" Icon="&#xE7A7;" />
    </rs:RibbonQuickAccessToolBar>
  </rs:Ribbon.QuickAccessToolBar>
</rs:Ribbon>
```

- **Add to QAT:** right-click any item → *Add to Quick Access Toolbar*, or call `ribbon.AddToQuickAccess(item)` /
  `AddToQuickAccess("bold")`.
  - The QAT shows a **linked copy**: checked states, values, labels and enabled state stay synchronized, and
    clicking the copy runs the original item.
  - Opt out per item with `CanAddToQuickAccess="False"`.
- **Remove:** right-click → *Remove from Quick Access Toolbar*, or `RemoveFromQuickAccess(item)`.
- **Customize menu** (the chevron):
  - checkable entries for `QuickAccessCandidateIds`
  - *More Commands...* (opens the customize dialog on the QAT page)
  - *Show Below/Above the Ribbon*
  - *Show Command Labels*
  - *Hide Quick Access Toolbar*
- **Placement:** above the ribbon (in the `RibbonTitleBar` when one is linked) or below the command bar.
- **Persistence:** `GetQuickAccessItemIds()`, and `RibbonState.QuickAccessItemIds` restores the QAT by id
  (copies that stay keep their links; removed copies are unlinked from their source item).
- **Reset:** the items and position declared by the application are captured on first load
  (`DefaultQuickAccessItemIds`, `DefaultQuickAccessPosition`); `ResetQuickAccess()` and the customize dialog's *Reset*
  restore them.
- **MVVM:** `RibbonModel.QuickAccessItems` and `QuickAccessCandidates`. UI changes are written back to the model.
- **KeyTips:** QAT items get numeric KeyTips `1`–`9`, then `09`, `08` … `01`, then `0A` … `0Z` (Office
  convention), continuing with `009` … for very long toolbars.
