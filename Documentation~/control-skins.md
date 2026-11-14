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

## Static cost ledger

Before execution: retain the existing 128-host, 8192-node/host, 128-control/host,
256-descendant/control, 64-depth and 32768-sample/query boundaries. The skin
pass observes neutral paint and five interaction states for both values of
boolean controls; at most 128 x 2 x 5 = 1280 styles updates/host. The existing
sample/query caps stop work before their larger Cartesian maxima. One native
panel and one copied theme live at a time. No layout or business input is run.

Attribution uses one native matched-rule query per sampled paint node, reused
for that node's properties during the current state. At most 4096 records/query
and 1048576 total declaration visits are admitted, with at most 32 native
longhands per declaration. Inherited lookups are bounded by 64 ancestors and
share the current-state cache. Bound cached nodes by 8192, properties by the
fixed paint-property set, and source text by asset paths and native property
names. Caches retire at every state transition and host disposal.

The existing 2000 ms observation budget remains a failing boundary. A whole
project is not certified by zero hosts, omitted generated parts or truncated
findings. Acceptance requires the original unskinned Foldout and raw controls
to fail, project-skinned controls and inherited/inline paint to pass, later
default-theme overrides to fail, and actual project calls to finish completely.
If attribution disagrees with these controls or cannot finish within its
bounds, stop and investigate the first failed contract before revising it.

Native attribution requires Unity 6000.6 or newer. Earlier declared package
targets still compile; opting into this capability there reports an explicit
unsupported-coverage error. Pixel aesthetics, contrast, composition and motion
also require inspection of the actual player surfaces.
