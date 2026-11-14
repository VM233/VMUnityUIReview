# Changelog

## [0.1.10] - 2026-11-14

- Exercise field input focus independently of the root, including hovered and
  pressed combinations. Retain root focus, boolean values and native budget caps.
  Catch generated Foldout arrows and captions overridden only after real clicks.

## [0.1.9] - 2026-11-14

- Distinguish the native USS importer's embedded Unity theme copies from
  project-authored styles and resources. Read UXML inline rules as well as IStyle.
- Review native field owners, both boolean values, generated inputs and custom
  root-owned fields. Keep inputless focusable containers outside field mutation.
- Reuse declaration provenance only for the same element and unchanged native
  matching-rules hash within one host. Verify enabled/disabled/return against
  uncached native attribution; use supported native pseudo classes in fixtures.
- With control-skin review enabled, continue complete native control observations
  between Editor updates for CLI, menu and automatic review. Retain all fields,
  states and total sample/query/declaration caps; publish only after retirement.
  Cancel incomplete work on source/domain changes, compilation, Play Mode,
  shutdown or timeout. Preserve Button-only execution and request-wide budget.

## [0.1.8] - 2026-11-14

- Preserve shared ancestor pseudo states across native feedback samples, with
  a real ancestor-resolution regression control; keep all coverage and budgets.

- Add the project-wide `rules.rejectDefaultControlAppearance` gate to native
  authored UI and current attached runtime resource review. Attribute actual
  paint to Unity's ordered style records, including generated parts, inherited
  and inline sources, boolean values, interaction and disabled states.
- Reject default-theme paint and built-in resources as errors even when a
  control has a skin class. Keep art, composition and runtime input acceptance
  separate, and report native capability/budget gaps as incomplete coverage.
- Retain raw Foldout, marker-only, partial checked-state, inline, inherited and
  project-paint controls beside the existing effective Button feedback suite.

## [0.1.7] - 2026-11-14

- Reuse the existing zero-border negative-control baseline in the structural
  scope fixture. A background override alone still permits the default theme's
  real hover border, so it cannot assert that both hover and press are absent.
  Keep the same required scope identities, query count and missing-state verdicts.

## [0.1.6] - 2026-11-14

- Replace the rejected current-state extractor with immutable structural selector
  projections evaluated by Unity's UQuery matcher. Preserve IDs, classes, types
  and parent relationships while removing pseudo restrictions only for scope.
- Index and deduplicate structural candidates once per request. Keep all real
  five-state paint verdicts and existing observation budgets; qualify conditional
  parent, state-only, disabled and overridden-feedback controls.

## [0.1.5] - 2026-11-14

- Introduced an imported-rule scope candidate on Unity 6000.6+. Native state-only
  qualification rejected it: current-state extraction omits inactive pseudo rules.
  The candidate is replaced in0.1.6; it did not qualify the original USS request.
- Preserve whole-template coverage for UXML and theme changes, the earlier-engine
  contract and all five-state paint verdicts and observation budgets. Report native
  match-query and unaffected-control counts; reject incomplete native capability.
- Qualify scope with native state-only, ancestor, descendant, unrelated, disabled
  and overridden-feedback controls alongside the existing cascade controls.

## [0.1.4] - 2026-11-14

- Exclude Unity object names and their prefab-variant overrides from the
  automatic prefab-to-UXML impact check. Gameplay objects named like an element
  no longer trigger unrelated full-layout audits. Serialized element paths,
  arbitrary binding fields, UXML GUID references and binding removal still count.
- Cover matching quoted/unquoted names, variant overrides, real binding controls
  and native prefab import/change/removal in the existing import fixture.

## [0.1.3] - 2026-11-13

- Include the five captured native root scales with failed Button feedback, so
  callers can distinguish a cascade failure from authored transform intent.
- Preserve UXML/USS diagnostic parity and qualify scale-only and inline override
  controls without changing feedback verdicts, native calls or budget gates.

## [0.1.2] - 2026-11-13

- Recognize proven named template-instance style variants before suggesting
  inline styles that would prevent those native overrides.
- Retain ordinary single-consumer geometry, unrelated ancestor, redundancy and
  native Button feedback checks; bound the per-audit provenance index.
- Cover ID/class targets and matched non-template, invariant and all-instance
  negative controls in the public USS self-test.

## [0.1.1] - 2026-11-12

- Require Automation 0.7.1's provider-owned profile validation.
- Read native computed border widths before layout; do not mistake unlaid-out
  resolved widths for zero-width styling. Retain real border-feedback and
  nonpainting zero-width negative controls.
- Reuse the existing unique native Type resolver for importer fixtures, including
  assemblies forwarding the same Type identity.

## [0.1.0] - 2026-11-12

- Extract UI authoring audits, import coordination, runtime experience core,
  style-reference review, transition analysis, UI Builder review and UI repair
  from VM Unity Automation 0.6.431. Preserve source asset GUIDs and established
  command identities with one provider and one implementation.
- Reject missing or cascade-blocked enabled Button hover and pressed feedback
  as errors. Resolve real imported themes/templates in an isolated native
  style panel; distinguish persistent focus from pressing and verify release.
- Include bounded coverage, incomplete-scope errors, native default-theme and
  negative controls. Automatic import reviews are enabled by default; explicit
  project policy remains visible in their status.
- Preserve UXML error severity through the public typed audit facade.
