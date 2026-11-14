# Project control skins

Projects that set `rules.rejectDefaultControlAppearance` reject Unity's painted
defaults in player UXML and attached runtime UI. Layout defaults remain usable.
The gate checks actual imported native parts, including generated children and
text, rather than requiring a particular class name or trusting a skin marker.
Project and package art/style sources own paint; Unity built-in images, fonts,
and winning default-theme colors or borders are errors. Identity tint and
transparent/nonpainting properties do not require decorative overrides.

Native style attribution follows Unity's
[Styles debugger](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.6/Modules/UIElementsEditor/Debugger/StylesDebugger.cs)
and [specified-style traversal](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.6/Modules/UIElements/Core/Style/StyleDebug.cs).
Unlike the retired pseudo-independent scope experiment, attribution deliberately
uses actual current pseudo states. Structural target scope still uses the
separate 0.1.7 projection. No duplicate CSS cascade or per-content exemption is
introduced. Missing native capability is incomplete coverage, not acceptance.

Unity's [USS importer](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.6/Editor/Mono/UIElements/StyleSheets/StyleSheetImporterImpl.cs)
embeds deep copies of `unity-theme://` styles and resources as subassets of the
owning USS/TSS. Their project path does not establish project authorship. The
gate rejects those embedded engine assets, and separately reads native UXML
inline rules instead of treating their absence from IStyle as an initial value.

## Static cost ledger

Before execution: retain the existing 128-host, 8192-node/host, 128-control/host,
256-descendant/control, 64-depth and 32768-sample/query boundaries. The skin
pass observes neutral paint and up to ten interaction states for both values of
boolean controls; at most 128 x 2 x 10 = 2560 styles updates/host. The existing
sample/query caps stop work before their larger Cartesian maxima. One native
panel and one copied theme live at a time. No layout or business input is run.

Attribution uses native matched-rule records for each sampled paint node. Reuse
is limited to the same node with an unchanged native `matchingRulesHash` inside
one host; unknown (`-1`) identities are always reread. This follows Unity's
[native style cache identity](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.6/Modules/UIElements/Core/VisualTreeStyleUpdater.cs).
Inline and inherited producers are still read for the current state. At most 4096 records/query
and 1048576 total declaration visits are admitted, with at most 32 native
longhands per declaration. Inherited lookups are bounded by 64 ancestors and
share the current-state cache. Bound cached nodes by 8192, properties by the
native declaration names, and source text by asset paths and native selectors.
Node caches retire at every host transition and request disposal; they never
cross nodes with merely equal style values or hashes.

The synchronous field matrix and conditional-engine projection candidates did
not qualify the full Profile request. They are retired. With control-skin review
enabled, CLI, menu and automatic review instead continue between Editor updates,
using the existing deferred-route pattern of native Builder preview. One step
observes a complete neutral host, one field's full value/state matrix, or one
Button's full matrix. Each step retains the 2000 ms active observation bound.
Button-only review keeps its request-wide bound and synchronous execution.
Total elapsed time is always reported; total sample/query/declaration caps remain
request-wide. No field, name, class, type or skin marker grants an exemption.
Acceptance requires raw Foldout, marker-only and partial-state controls to fail,
project/inline/inherited paint to pass, and complete current-project requests.

A continuation admits at most eight owned native contexts, runs at most8ms of
steps per Editor update (one native step is indivisible), and retires after60s.
No result is published before all steps complete. Source changes, Play Mode,
compilation, domain retirement, shutdown, observation failure and cancellation
mark coverage incomplete and dispose the native iterator/panel before publishing.
The source-change event invalidates the earlier graph rather than silently
adopting changed assets. Normal finish, cancellation and failure retain native
panel-retirement controls. Synchronous C# consumers drain the same step iterator.
These execution bounds do not replace actual style and player-visual acceptance.

Native fields use the actual BaseField owner, rather than every focusable
container. Boolean fields include RadioButton, Toggle and custom root-owned
fields. Imported controls that deliberately remove the native field class own
their root interaction. An actual native field class still requires its input.
Root and input focus are sampled independently, including focused hover and
focused press. The original root-only focus missed Unity's input-focused Foldout
arrow and caption overrides after real pointer input. This follows Unity's
[Styles Explorer input-state sampling](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/UIElementsSamplesEditor/StylesExplorer.cs).
The extra axis retains the existing total sample/query/declaration caps and
per-observation budget; exceeding any bound fails completeness. Require native
input-focus negative controls and replay the attached pointer-click witness.
Invisible generated paint is not accepted as visible art. Runtime review checks
the current attached state; popup, selection/caret paint, motion and other
application-generated states still require their real entry and visual review.

Native attribution requires Unity 6000.6 or newer. Earlier declared package
targets still compile; opting into this capability there reports an explicit
unsupported-coverage error. Pixel aesthetics, contrast, composition and motion
also require inspection of the actual player surfaces.
