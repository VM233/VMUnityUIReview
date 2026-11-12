# Changelog

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
