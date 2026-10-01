#!/usr/bin/env python3
"""Generates docs/controls.md (controls reference) from the XML doc comments of RibbonSpace.Uno.

Run from the repository root:  python3 tools/generate-api-reference.py
"""
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src/RibbonSpace.Uno"
GEN = SRC / "Controls/Generated"
ORDER = [
    "Ribbon", "RibbonTab", "RibbonTabHeader", "RibbonContextualTabGroup", "RibbonGroup",
    "RibbonButton", "RibbonDropDownButton", "RibbonSplitButton", "RibbonToggleButton", "RibbonCheckBox",
    "RibbonComboBox", "RibbonFontComboBox", "RibbonFontSizeComboBox", "RibbonSpinner", "RibbonTextBox", "RibbonSlider",
    "RibbonGallery", "RibbonGalleryItem", "RibbonColorPicker", "RibbonColorPalette", "RibbonGridPicker",
    "RibbonSegmentedControl", "RibbonSegment", "RibbonButtonGroup", "RibbonStackPanel", "RibbonSeparator", "RibbonLabel",
    "RibbonQuickAccessToolBar", "RibbonBackstage", "RibbonBackstageItem", "RibbonScreenTip", "RibbonKeyTip",
    "RibbonSearchBox", "RibbonTitleBar", "RibbonStatusBar", "RibbonZoomControl",
    "RibbonToolBar", "RibbonContextualToolBar", "RibbonMenuBar", "RibbonMenuBarItem",
    "RibbonTheme", "RibbonThemeResources", "RibbonCustomizeDialog", "RibbonCommandPalette",
]

member_re = re.compile(r"^\s*public (?:static )?(?:override |virtual |new |sealed |abstract |async )*(?:event )?([\w<>\[\],.? ]+?) (\w+)(\s*\{|\s*=>|\s*\(|\s*;)")
class_re = re.compile(r"^\s*(?:public )?(?:sealed |abstract |static )*(?:partial )?class (\w+)(?:\s*:\s*([^\n{]+))?")

def summaries(path):
    lines = path.read_text().splitlines()
    current = None
    doc = []
    result = {}
    bases = {}
    for line in lines:
        s = line.strip()
        if s.startswith("///"):
            text = re.sub(r'<see (?:c|lang)?ref="(?:[A-Z]:)?([^"]+)"\s*/>', lambda m: "`" + m.group(1).split(".")[-1] + "`", s[3:].strip())
            text = re.sub(r"<c>(.*?)</c>", r"`\1`", text)
            doc.append(re.sub(r"<[^>]+>", "", text))
            continue
        c = class_re.match(line)
        if c:
            current = c.group(1)
            result.setdefault(current, {"doc": " ".join(d for d in doc if d).strip(), "members": []})
            if c.group(2):
                bases[current] = c.group(2).split(",")[0].strip()
            doc = []
            continue
        m = member_re.match(line)
        if m and current and not m.group(2).endswith("Property") and "DependencyProperty" not in line and m.group(2) != current:
            kind = "event" if " event " in line else ("method" if m.group(3).strip() == "(" else "property")
            text = " ".join(d for d in doc if d).strip()
            if text and not text.startswith("inheritdoc"):
                result[current]["members"].append((kind, m.group(2), m.group(1).strip(), text))
        if s and not s.startswith("["):
            doc = []
    return result, bases

classes = {}
bases = {}
for path in sorted(SRC.rglob("*.cs")):
    if "/obj/" in str(path) or "/bin/" in str(path):
        continue
    found, b = summaries(path)
    bases.update(b)
    for name, info in found.items():
        entry = classes.setdefault(name, {"doc": "", "members": []})
        entry["doc"] = entry["doc"] or info["doc"]
        entry["members"].extend(info["members"])

common = [(k, n, t, d) for (k, n, t, d) in summaries(GEN / "RibbonControlBase.Common.g.cs")[0]["RibbonControlBase"]["members"]]
out = ["# Controls reference", "",
       "Generated from the source XML documentation by `tools/generate-api-reference.py`.", "",
       "## Common item properties", "",
       "Every item control (`RibbonButton`, `RibbonToggleButton`, `RibbonCheckBox` and all `RibbonControlBase` items) implements `IRibbonItem` and has:", "",
       "| Member | Type | Description |", "|---|---|---|"]
for kind, name, typ, text in common:
    out.append(f"| `{name}` | `{typ}` | {text} |")
out += ["", "Methods: `ApplyLayout(RibbonItemLayout)`, `IRibbonItem.CreateLinkedCopy()`, `IRibbonItem.CreateOverflowMenuItems()`, `IRibbonItem.OnKeyTip()`, `IRibbonItem.Invoke()`.", ""]
for name in ORDER:
    if name not in classes:
        continue
    info = classes[name]
    base = bases.get(name, "")
    out.append(f"## {name}")
    out.append("")
    if base:
        out.append(f"*Base:* `{base}`")
        out.append("")
    if info["doc"]:
        out.append(info["doc"])
        out.append("")
    members = [m for m in info["members"] if m[1] not in {c[1] for c in common}]
    if members:
        out.append("| Member | Kind | Type | Description |")
        out.append("|---|---|---|---|")
        seen = set()
        for kind, mname, typ, text in members:
            if (kind, mname) in seen:
                continue
            seen.add((kind, mname))
            out.append(f"| `{mname}` | {kind} | `{typ}` | {text} |")
        out.append("")
(ROOT / "docs/controls.md").write_text("\n".join(out) + "\n")
print("wrote docs/controls.md with", sum(1 for n in ORDER if n in classes), "controls")
