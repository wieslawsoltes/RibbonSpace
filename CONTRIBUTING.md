# Contributing

Thanks for helping improve RibbonSpace!

1. Discuss larger changes in an issue first.
2. Keep the code style: file-scoped namespaces, XML documentation on public members, nullable enabled, no warnings.
3. Put UI-independent logic in `RibbonSpace.Core` and cover it with unit tests (`tests/RibbonSpace.Core.Tests`).
4. Cover control behaviour with runtime UI tests (`tests/RibbonSpace.Uno.RuntimeTests`).
5. Keep controls lookless. Visuals go in `src/RibbonSpace.Uno/Themes/*.xaml`, colours in theme brush keys.
6. The shared item properties are generated. Edit `tools/generate-item-common.py` and run it; do not edit
   `Controls/Generated/*`.
7. After API changes, update the docs and run `python3 tools/generate-api-reference.py`.
8. Check the result visually with the screenshot automation described in [docs/testing.md](docs/testing.md).

```bash
dotnet build RibbonSpace.slnx
dotnet test tests/RibbonSpace.Core.Tests
dotnet run --project tests/RibbonSpace.Uno.RuntimeTests -f net10.0-desktop
```
