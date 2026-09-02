# Transactions Mockup Design Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Align `TransactionsPage` visuals with the Aug 2026 mockup: chip month switcher (incl. Gesamt → search path) and list contrast (day cards, category-colored icon chips) without restructuring search/filter/templates/FABs.

**Architecture:** Extend month/search use-case results with `ColorMap`; drive chip labels/mode from `TransactionsViewModel` (`IsGesamtMode` folds into `IsSearchActive`); render the list as a non-grouped `CollectionView` of `TransactionGroup` cards (`BindableLayout` rows inside) to match mockup day cards without nested `CollectionView`.

**Tech Stack:** .NET 10 MAUI, CommunityToolkit.Mvvm, xUnit, NSubstitute, existing `Colors.xaml` / resx / ResourceKeys.

## Global Constraints

- Spec: `docs/superpowers/specs/2026-08-08-transactions-mockup-design.md`
- Design-only except month chip strip; no Create-UX / #266 / FAB behavior changes
- Reuse `Category.Color`; fallback `#8E8E93`
- Chips: `[Vormonat] [Aktuell] [Nächster] [Gesamt]`
- Gesamt = existing `SearchTransactionsUseCase` (unbounded dates)
- Prefer no Nested-CollectionView; day card via group-as-item + `BindableLayout`
- Commits only when the user asks (do not auto-commit unless requested)

---

## File map

| File | Responsibility |
|------|----------------|
| `Finanzuebersicht/Resources/Styles/Colors.xaml` | `PrimaryTint` / `PrimaryTintDark` |
| `Finanzuebersicht.Application/.../LoadTransactionsMonthUseCase.cs` | Add `ColorMap` to result |
| `Finanzuebersicht.Application/.../SearchTransactionsUseCase.cs` | Add `ColorMap` to result |
| `Finanzuebersicht/Converters/KategorieIdToColorConverter.cs` | Id + ColorMap → `Color` |
| `Finanzuebersicht/Converters/KategorieIdToNameConverter.cs` | Id + CategoryNameMap → name |
| `Finanzuebersicht.Presentation/ViewModels/MonthNavigationViewModel.cs` | Hook for chip label refresh |
| `Finanzuebersicht.Presentation/ViewModels/TransactionsViewModel.cs` | Chips, `IsGesamtMode`, ColorMap, search |
| `Finanzuebersicht/Views/TransactionsPage.xaml` | Chip strip + card list templates |
| `Finanzuebersicht/Resources/Strings/AppResources*.resx` + `ResourceKeys.cs` | `Lbl_Gesamt`, A11y |
| `Finanzuebersicht.Tests/.../LoadTransactionsMonthUseCaseTests.cs` | ColorMap assert |
| `Finanzuebersicht.Tests/.../SearchTransactionsUseCaseTests.cs` | ColorMap assert (if file exists / add) |
| `Finanzuebersicht.Tests/ViewModels/TransactionsViewModelTests.cs` | Gesamt / ClearSearch |

---

### Task 1: ColorMap in month + search use cases

**Files:**
- Modify: `Finanzuebersicht.Application/UseCases/Transactions/LoadTransactionsMonthUseCase.cs`
- Modify: `Finanzuebersicht.Application/UseCases/Transactions/SearchTransactionsUseCase.cs`
- Modify: `Finanzuebersicht.Tests/Application/UseCases/LoadTransactionsMonthUseCaseTests.cs`
- Modify or create: `Finanzuebersicht.Tests/Application/UseCases/SearchTransactionsUseCaseTests.cs`

**Interfaces:**
- Produces: `TransactionsMonthData.ColorMap` and `SearchTransactionsResult.ColorMap` as `Dictionary<string, string>` (category Id → hex, from `Category.Color`, empty string → skip or still include)

- [ ] **Step 1: Write the failing test**

In `LoadTransactionsMonthUseCaseTests.cs` add:

```csharp
[Fact]
public async Task ExecuteAsync_BuildsColorMapFromCategories()
{
    var transactionRepository = Substitute.For<ITransactionRepository>();
    var categoryRepository = Substitute.For<ICategoryRepository>();
    var accountRepository = Substitute.For<IAccountRepository>();

    transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
        .Returns(new List<Transaction>());

    categoryRepository.GetCategoriesAsync().Returns(new List<Category>
    {
        new() { Id = "c1", Icon = "🍔", Color = "#34C759" },
        new() { Id = "c2", Icon = "🚗", Color = "#FF9500" }
    });
    accountRepository.GetAccountsAsync().Returns(new List<Account>());

    var useCase = new LoadTransactionsMonthUseCase(transactionRepository, categoryRepository, accountRepository);
    var result = await useCase.ExecuteAsync(new DateTime(2026, 3, 1));

    Assert.Equal("#34C759", result.ColorMap["c1"]);
    Assert.Equal("#FF9500", result.ColorMap["c2"]);
}
```

Mirror the same assert in `SearchTransactionsUseCaseTests` (create file if missing) calling `ExecuteAsync(new SearchTransactionsQuery())`.

- [ ] **Step 2: Run tests to verify they fail**

Run:

```bash
dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj --filter "FullyQualifiedName~LoadTransactionsMonthUseCaseTests.ExecuteAsync_BuildsColorMapFromCategories" --no-restore
```

Expected: FAIL (e.g. `ColorMap` does not exist).

- [ ] **Step 3: Minimal implementation**

In both use cases, after building `iconMap` / `categoryNameMap`:

```csharp
var colorMap = categories.ToDictionary(c => c.Id, c => string.IsNullOrWhiteSpace(c.Color) ? "#8E8E93" : c.Color);
```

Add property to both result types:

```csharp
public Dictionary<string, string> ColorMap { get; set; } = [];
```

Assign `ColorMap = colorMap` in the returned object.

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj --filter "FullyQualifiedName~LoadTransactionsMonthUseCaseTests|FullyQualifiedName~SearchTransactionsUseCaseTests" --no-restore
```

Expected: PASS for ColorMap tests; existing tests still green.

- [ ] **Step 5: Commit** (only if user asked)

```bash
git add Finanzuebersicht.Application/UseCases/Transactions/LoadTransactionsMonthUseCase.cs \
  Finanzuebersicht.Application/UseCases/Transactions/SearchTransactionsUseCase.cs \
  Finanzuebersicht.Tests/Application/UseCases/LoadTransactionsMonthUseCaseTests.cs \
  Finanzuebersicht.Tests/Application/UseCases/SearchTransactionsUseCaseTests.cs
git commit -m "$(cat <<'EOF'
feat(transactions): include category ColorMap in month/search results

EOF
)"
```

---

### Task 2: Color + name converters

**Files:**
- Create: `Finanzuebersicht/Converters/KategorieIdToColorConverter.cs`
- Create: `Finanzuebersicht/Converters/KategorieIdToNameConverter.cs`
- Test: optional small unit tests under `Finanzuebersicht.Tests/Converters/` if the project already tests converters; otherwise rely on UI binding + use-case map (skip dedicated test project if none exists)

**Interfaces:**
- Consumes: `IDictionary<string, string>` ColorMap / CategoryNameMap
- Produces: `KategorieIdToColorConverter.Convert` → `Color`; `KategorieIdToNameConverter.Convert` → `string`

- [ ] **Step 1: Add converters**

`KategorieIdToColorConverter.cs`:

```csharp
using System.Globalization;

namespace Finanzuebersicht.Converters;

public class KategorieIdToColorConverter : IMultiValueConverter
{
    private static readonly Color Fallback = Color.FromArgb("#8E8E93");

    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 1 || values[0] is not string kategorieId || string.IsNullOrEmpty(kategorieId))
            return Fallback;

        if (values.Length >= 2 && values[1] is IDictionary<string, string> colorMap
            && colorMap.TryGetValue(kategorieId, out var hex)
            && !string.IsNullOrWhiteSpace(hex))
        {
            try { return Color.FromArgb(hex); }
            catch { return Fallback; }
        }

        return Fallback;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
```

`KategorieIdToNameConverter.cs` — same shape as `KategorieIdToIconConverter`, return `string.Empty` or `"—"` when missing (prefer empty string).

- [ ] **Step 2: Build to ensure converters compile**

```bash
dotnet build Finanzuebersicht/Finanzuebersicht.csproj -f net10.0-maccatalyst --no-restore
```

Expected: Build succeeded (or restore first if needed).

- [ ] **Step 3: Commit** (only if user asked)

---

### Task 3: ViewModel — chip labels, IsGesamtMode, ColorMap wiring

**Files:**
- Modify: `Finanzuebersicht.Presentation/ViewModels/MonthNavigationViewModel.cs`
- Modify: `Finanzuebersicht.Presentation/ViewModels/TransactionsViewModel.cs`
- Modify: `Finanzuebersicht.Presentation/Resources/Strings/ResourceKeys.cs`
- Modify: `Finanzuebersicht/Resources/Strings/AppResources.resx`
- Modify: `Finanzuebersicht/Resources/Strings/AppResources.de.resx`
- Modify: `Finanzuebersicht.Tests/ViewModels/TransactionsViewModelTests.cs`

**Interfaces:**
- Consumes: `ColorMap` from use-case results (Task 1)
- Produces:
  - `bool IsGesamtMode`
  - `bool IsSearchActive` includes `IsGesamtMode`
  - `string VormonatChipLabel`, `NaechsterChipLabel` (computed)
  - `IRelayCommand EnterGesamtModeCommand`
  - `Dictionary<string, string> ColorMap`
  - `ClearSearch` clears `IsGesamtMode`

- [ ] **Step 1: Write failing VM tests**

```csharp
[Fact]
public async Task EnterGesamtMode_ActivatesSearchPathAndLoadsAll()
{
    // Arrange CreateSut with searchTransactionRepository returning one tx
    // ...
    Assert.False(viewModel.IsSearchActive);

    await viewModel.EnterGesamtModeCommand.ExecuteAsync(null);

    Assert.True(viewModel.IsGesamtMode);
    Assert.True(viewModel.IsSearchActive);
    Assert.False(viewModel.IsMonthMode);
    await searchTransactionRepository.Received().GetTransactionsAsync(
        Arg.Is<DateTime>(d => d == DateTime.MinValue),
        Arg.Is<DateTime>(d => d == DateTime.MaxValue));
}

[Fact]
public async Task ClearSearch_ResetsIsGesamtMode()
{
    var viewModel = CreateSut(/* ... */);
    await viewModel.EnterGesamtModeCommand.ExecuteAsync(null);
    await viewModel.ClearSearchCommand.ExecuteAsync(null);

    Assert.False(viewModel.IsGesamtMode);
    Assert.True(viewModel.IsMonthMode);
}

[Fact]
public async Task PreviousMonth_WhileGesamt_ExitsGesamtAndShowsMonth()
{
    var viewModel = CreateSut(/* ... */);
    var before = viewModel.AktuellerMonat;
    await viewModel.EnterGesamtModeCommand.ExecuteAsync(null);
    await viewModel.PreviousMonthCommand.ExecuteAsync(null);

    Assert.False(viewModel.IsGesamtMode);
    Assert.Equal(before.AddMonths(-1), viewModel.AktuellerMonat);
    Assert.True(viewModel.IsMonthMode);
}
```

Wire `CreateSut` the same way as existing tests (copy arrange from `ClearSearch_ResetsAllFiltersAndReloads`).

- [ ] **Step 2: Run tests — expect FAIL**

```bash
dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj --filter "FullyQualifiedName~EnterGesamtMode|FullyQualifiedName~ClearSearch_ResetsIsGesamtMode|FullyQualifiedName~PreviousMonth_WhileGesamt" --no-restore
```

- [ ] **Step 3: MonthNavigationViewModel hook**

```csharp
protected void UpdateMonatAnzeige()
{
    MonatAnzeige = AktuellerMonat.ToString("MMMM yyyy",
        System.Globalization.CultureInfo.CurrentCulture);
    OnMonatAnzeigeUpdated();
}

protected virtual void OnMonatAnzeigeUpdated() { }
```

- [ ] **Step 4: TransactionsViewModel changes**

Add resources:

```xml
<!-- AppResources.de.resx -->
<data name="Lbl_Gesamt" xml:space="preserve"><value>Gesamt</value></data>
<data name="A11y_GesamtAnzeigen" xml:space="preserve"><value>Alle Transaktionen anzeigen</value></data>
<!-- AppResources.resx (EN) -->
<data name="Lbl_Gesamt" xml:space="preserve"><value>All</value></data>
<data name="A11y_GesamtAnzeigen" xml:space="preserve"><value>Show all transactions</value></data>
```

`ResourceKeys.cs`: `Lbl_Gesamt`, `A11y_GesamtAnzeigen`.

VM:

```csharp
[ObservableProperty]
private Dictionary<string, string> colorMap = [];

[ObservableProperty]
[NotifyPropertyChangedFor(nameof(IsSearchActive))]
[NotifyPropertyChangedFor(nameof(IsMonthMode))]
[NotifyPropertyChangedFor(nameof(ShowTransactionTemplates))]
[NotifyPropertyChangedFor(nameof(IsGesamtChipActive))]
[NotifyPropertyChangedFor(nameof(IsCurrentMonthChipActive))]
private bool isGesamtMode;

public bool IsSearchActive =>
    IsGesamtMode ||
    !string.IsNullOrWhiteSpace(SearchText) ||
    IsFilterActive;

public bool IsCurrentMonthChipActive => IsMonthMode;
public bool IsGesamtChipActive => IsSearchActive; // typing/filter/Gesamt all highlight Gesamt

public string VormonatChipLabel =>
    AktuellerMonat.AddMonths(-1).ToString("MMMM", System.Globalization.CultureInfo.CurrentCulture);

public string NaechsterChipLabel =>
    AktuellerMonat.AddMonths(1).ToString("MMMM", System.Globalization.CultureInfo.CurrentCulture);

public string GesamtChipLabel => _loc.GetString(ResourceKeys.Lbl_Gesamt);

protected override void OnMonatAnzeigeUpdated()
{
    OnPropertyChanged(nameof(VormonatChipLabel));
    OnPropertyChanged(nameof(NaechsterChipLabel));
}

protected override async Task OnMonthChangedAsync()
{
    if (IsGesamtMode)
        IsGesamtMode = false;
    await LoadTransaktionen();
}

[RelayCommand]
private async Task EnterGesamtMode()
{
    IsGesamtMode = true;
    await ExecuteSearchAsync();
}

// In ClearSearch, before LoadTransaktionen:
IsGesamtMode = false;

// When assigning maps in LoadTransaktionen / ExecuteSearchAsync:
ColorMap = result.ColorMap; // or data.ColorMap
```

Also notify `IsCurrentMonthChipActive` / `IsGesamtChipActive` wherever `IsSearchActive` already notifies (existing `[NotifyPropertyChangedFor(nameof(IsSearchActive))]` sites — add the two chip props there, or rely on `IsGesamtMode` / search fields).

- [ ] **Step 5: Run VM tests — expect PASS**

```bash
dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj --filter "FullyQualifiedName~TransactionsViewModelTests" --no-restore
```

- [ ] **Step 6: Commit** (only if user asked)

---

### Task 4: Colors + TransactionsPage XAML (chips + cards)

**Files:**
- Modify: `Finanzuebersicht/Resources/Styles/Colors.xaml`
- Modify: `Finanzuebersicht/Views/TransactionsPage.xaml`

**Interfaces:**
- Consumes: VM chip props/commands, `ColorMap`, converters from Task 2
- Produces: Updated UI only

- [ ] **Step 1: Add tint colors**

In `Colors.xaml`:

```xml
<Color x:Key="PrimaryTint">#26007AFF</Color>
<Color x:Key="PrimaryTintDark">#260A84FF</Color>
```

(`#26` ≈ 15% alpha, matches mockup `rgba(10,132,255,0.15)`.)

- [ ] **Step 2: Replace month nav row with chips**

Replace Grid Row 1 (`‹` / label / `›`) with a horizontal chip strip **always visible** (remove `IsVisible={Binding IsMonthMode}` so Gesamt stays reachable):

```xml
<ScrollView Grid.Row="1" Orientation="Horizontal" HorizontalScrollBarVisibility="Never"
            Padding="16,8,16,8">
  <HorizontalStackLayout Spacing="8">
    <!-- Vormonat -->
    <Border StrokeShape="RoundRectangle 99" Stroke="Transparent" Padding="14,7"
            BackgroundColor="{AppThemeBinding Light={StaticResource CardBackground}, Dark={StaticResource CardBackgroundDark}}">
      <Border.GestureRecognizers>
        <TapGestureRecognizer Command="{Binding PreviousMonthCommand}" />
      </Border.GestureRecognizers>
      <Label Text="{Binding VormonatChipLabel}" FontSize="13" FontAttributes="Bold"
             TextColor="{AppThemeBinding Light={StaticResource TextTertiary}, Dark={StaticResource Gray600}}"
             SemanticProperties.Description="{loc:Translate A11y_VorherigerMonat}" />
    </Border>
    <!-- Aktuell -->
    <Border StrokeShape="RoundRectangle 99" Stroke="Transparent" Padding="14,7">
      <Border.Triggers>
        <DataTrigger TargetType="Border" Binding="{Binding IsCurrentMonthChipActive}" Value="True">
          <Setter Property="BackgroundColor" Value="{AppThemeBinding Light={StaticResource PrimaryTint}, Dark={StaticResource PrimaryTintDark}}" />
        </DataTrigger>
        <DataTrigger TargetType="Border" Binding="{Binding IsCurrentMonthChipActive}" Value="False">
          <Setter Property="BackgroundColor" Value="{AppThemeBinding Light={StaticResource CardBackground}, Dark={StaticResource CardBackgroundDark}}" />
        </DataTrigger>
      </Border.Triggers>
      <Label Text="{Binding MonatAnzeige}" FontSize="13" FontAttributes="Bold"
             TextColor="{AppThemeBinding Light={StaticResource Primary}, Dark={StaticResource PrimaryDark}}" />
    </Border>
    <!-- Nächster — same card style, Command=NextMonthCommand, A11y_NaechsterMonat -->
    <!-- Gesamt — accent via IsGesamtChipActive, Command=EnterGesamtModeCommand, A11y_GesamtAnzeigen -->
  </HorizontalStackLayout>
</ScrollView>
```

Complete Nächster/Gesamt Borders analogously (Gesamt label `{Binding GesamtChipLabel}`).

- [ ] **Step 3: Day/month cards — switch off IsGrouped**

Month `CollectionView`:

- `IsGrouped="False"` (remove)
- `ItemsSource="{Binding TransaktionsGruppen}"` stays
- `ItemTemplate`: one `Border` card per `TransactionGroup`:

```xml
<DataTemplate x:DataType="models:TransactionGroup">
  <VerticalStackLayout Padding="16,0,16,12" Spacing="8">
    <Label Text="{Binding DatumFormatiert}"
           FontSize="12" FontAttributes="Bold"
           TextTransform="Uppercase"
           TextColor="{AppThemeBinding Light={StaticResource TextTertiary}, Dark={StaticResource Gray600}}"
           Margin="6,8,6,0" />
    <Border StrokeShape="RoundRectangle 18" Stroke="Transparent"
            BackgroundColor="{AppThemeBinding Light={StaticResource CardBackground}, Dark={StaticResource CardBackgroundDark}}"
            Padding="4,0">
      <VerticalStackLayout BindableLayout.ItemsSource="{Binding .}">
        <BindableLayout.ItemTemplate>
          <DataTemplate x:DataType="models:Transaction">
            <SwipeView>
              <!-- existing RightItems -->
              <Grid ColumnDefinitions="44,*,Auto" Padding="14,10" ColumnSpacing="12">
                <!-- tap → GoToDetailCommand -->
                <Border Grid.Column="0" WidthRequest="38" HeightRequest="38"
                        StrokeShape="RoundRectangle 11" Stroke="Transparent">
                  <Border.BackgroundColor>
                    <MultiBinding Converter="{StaticResource KategorieIdToColor}">
                      <Binding Path="KategorieId" />
                      <Binding Source="{RelativeSource AncestorType={x:Type vm:TransactionsViewModel}}" Path="ColorMap" />
                    </MultiBinding>
                  </Border.BackgroundColor>
                  <Label FontSize="18" HorizontalOptions="Center" VerticalOptions="Center">
                    <Label.Text>
                      <MultiBinding Converter="{StaticResource KategorieIdToIcon}">
                        <Binding Path="KategorieId" />
                        <Binding Source="{RelativeSource AncestorType={x:Type views:BaseContentPage}}" Path="BindingContext.IconMap" />
                      </MultiBinding>
                    </Label.Text>
                  </Label>
                </Border>
                <VerticalStackLayout Grid.Column="1" Spacing="2" VerticalOptions="Center">
                  <Label Text="{Binding Titel}" FontSize="15" FontAttributes="Bold" LineBreakMode="TailTruncation" />
                  <Label FontSize="12"
                         TextColor="{AppThemeBinding Light={StaticResource TextTertiary}, Dark={StaticResource Gray600}}"
                         LineBreakMode="TailTruncation">
                    <Label.Text>
                      <MultiBinding Converter="{StaticResource KategorieIdToName}">
                        <Binding Path="KategorieId" />
                        <Binding Source="{RelativeSource AncestorType={x:Type vm:TransactionsViewModel}}" Path="CategoryNameMap" />
                      </MultiBinding>
                    </Label.Text>
                  </Label>
                </VerticalStackLayout>
                <Label Grid.Column="2"
                       Text="{Binding ., Converter={StaticResource BetragDisplay}}"
                       TextColor="{Binding Typ, Converter={StaticResource TypToColor}}"
                       FontSize="15" FontAttributes="Bold"
                       VerticalTextAlignment="Center" />
              </Grid>
            </SwipeView>
          </DataTemplate>
        </BindableLayout.ItemTemplate>
      </VerticalStackLayout>
    </Border>
  </VerticalStackLayout>
</DataTemplate>
```

Register converters in page resources:

```xml
<conv:KategorieIdToColorConverter x:Key="KategorieIdToColor" />
<conv:KategorieIdToNameConverter x:Key="KategorieIdToName" />
```

Apply the **same card ItemTemplate** to search `CollectionView` (`SearchErgebnisGruppen`, also `IsGrouped="False"`). Remove obsolete shared flat `TransactionRowTemplate` or repoint it to avoid drift — Prefer delete unused template after both lists use the card template (can use `x:Key` shared `DataTemplate` for groups).

EmptyView / RefreshView / FABs: leave as-is.

- [ ] **Step 4: Build Mac Catalyst target**

```bash
dotnet build Finanzuebersicht/Finanzuebersicht.csproj -f net10.0-maccatalyst
```

Expected: success. Fix XAML bind errors if any (`AncestorType` for VM from inside `BindableLayout` — if RelativeSource fails, bind via `Source={RelativeSource AncestorType={x:Type views:BaseContentPage}} Path=BindingContext.ColorMap` like existing IconMap).

- [ ] **Step 5: Manual smoke (Abnahme)**

- Month chips: back/forward change month; accent on current
- Gesamt: all transactions via search list; Gesamt chip accent; ClearSearch / month chip returns
- Category icon backgrounds colored; day cards rounded; headers uppercase
- Swipe delete/duplicate still works
- Search bar + filter + templates + FABs still present

- [ ] **Step 6: Commit** (only if user asked)

```bash
git commit -m "$(cat <<'EOF'
feat(transactions): mockup chips, day cards, category color icons

EOF
)"
```

---

## Spec coverage checklist

| Spec item | Task |
|-----------|------|
| Chip strip Vormonat/Aktuell/Nächster/Gesamt | 3, 4 |
| Gesamt → Search use case | 3 |
| Clear / month exits Gesamt | 3 |
| ColorMap + colored chips | 1, 2, 4 |
| Day cards + uppercase headers | 4 |
| Subtitle = category name | 4 |
| No Create-UX / layout chrome reshuffle | 4 (unchanged rows 0,2,3,4, FABs) |
| Primary tint token | 4 |
| Tests VM + ColorMap | 1, 3 |

## Placeholder / consistency self-review

- No TBD left; `Nächster` chip included for parity with arrows
- `ColorMap` name consistent across use cases, VM, converters
- `IsGesamtChipActive => IsSearchActive` so filter/typing also accents Gesamt (matches “Gesamt = non-month path”)

---

## Execution handoff

Plan complete and saved to `docs/superpowers/plans/2026-08-08-transactions-mockup-design.md`.

**Two execution options:**

1. **Subagent-Driven (recommended)** — fresh subagent per task, review between tasks  
2. **Inline Execution** — run tasks in this session with checkpoints  

Which approach?
