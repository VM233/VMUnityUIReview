# Native button feedback gate

An enabled authored Button needs an effective hover presentation and a distinct
pressed presentation. The gate reports errors for missing or overridden feedback,
including opaque text buttons, inline/ID/later cascade overrides, identical hover
and active styles, and focus-only feedback. It also rejects incomplete capability,
theme, hierarchy or budget coverage. Disabled controls are counted separately.

The imported native VisualTreeAsset expands templates in their complete host.
A separate offscreen native Panel adopts a copy of the actual PanelSettings
ThemeStyleSheet with its default-sheet origin. It creates no EditorWindow,
dispatches no business input and ticks only the Styles updater. Clone-only zero
transition durations measure stable target values. Real motion is reviewed through
actual controls. The panel, cloned theme and ScriptableObject owner retire in a
finally/Dispose lifetime, including exceptions.

Five states are measured per enabled control: neutral, hover, focused hover,
focused press and released focused hover. Hover must differ from neutral; press
must differ from the same focused hover, so persistent focus cannot qualify.
Press may restore normal paint when hover changed it. The button and its visual
descendants contribute resolved paint, border, opacity, transform, text and native
image sizing. Authoring checks never infer actual readable contrast, hit targets,
custom drawing or callback behavior from these target values.

Border widths use native ComputedStyle getters. IResolvedStyle border widths
read layout results and can still be zero after only Styles has updated; using
those values would incorrectly reject legitimate border-color feedback. Four
typed delegates call the exact native computed-style and width getters, without
running layout callbacks or guessing from inline declarations. The supported
native ABI is checked before use; capability failure rejects coverage.

Sources: Unity's [Panel implementation](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.0/Modules/UIElements/Core/Panel.cs),
[PanelSettings theme adoption](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.0/Modules/UIElements/Core/GameObjects/PanelSettings.cs),
and [pseudo-state semantics](https://docs.unity.com/en-us/engine/6000.0/manual/uitoolkits/uielements/uie-uss/selectors/pseudo-classes).
An unavailable exact native member rejects coverage; it does not switch to an
approximate declaration scanner.

## Static cost ledger

Index at most128 UXML assets and4096 native dependencies per host: at most524288
retained dependency paths. Native clone traversal has8192 nodes/host,128 Buttons,
256 sampled descendants/control and depth64. Five styles updates per control give
at most640 updates/host; a global32768-sample budget further bounds all hosts.
Current-control snapshots retain at most5×256×18 boxed values (about1 MiB,
platform allocation overhead dependent), plus one host tree and one64-node state
chain. One native panel/theme/owner is alive at once. Provider initialization reads
exactly12 contract files, each limited to1 MiB, once per domain.

Computed borders add four fixed native getter calls per visual sample, at most
131072 calls under the existing sample cap, with four delegates created once per
domain. They add no data loop, retained snapshot field or layout pass.

The2000 ms main-thread observation budget is checked between native calls; it is
not a preemptive timeout inside an engine operation. Over-budget results are
incomplete errors. Native dependency/style work is measured by the real request;
the bound does not promise a fixed unit cost inside Unity. Runtime-created controls
are outside authored coverage and remain explicit in the report.

Native fixtures retain default-theme, missing, hover-only, identical, cascade,
inline, persistent-focus, descendant, return-to-normal and disabled controls.
Manual visual review remains required even when all measured states differ.

Failed-state diagnostics retain the root control's native scale in all five
captured states as invariant round-trip x,y,z strings. These are the already
measured values, not authored intent; they do not change the verdict or stand in
for descendant paint. The snapshot adds one Vector3 (12 bytes), at most15 KiB
across the existing five-by256 live samples. Failure output adds five short
strings per issue under the existing5000-issue cap (under2 MiB numeric/key payload).
There are no additional native getters, scans, update passes or relaxed budgets.
Native scale-only positive and inline-scale negative fixtures qualify this
observation before callers use it to distinguish cascade from capture failures.
