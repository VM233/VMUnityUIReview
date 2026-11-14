# UI review references

- [Native button feedback and coverage](button-feedback.md)
- [Project control skins and default-theme rejection](control-skins.md)
- [USS and UXML authoring](uitoolkit-audits.md)
- [Template instance style ownership](template-instance-styles.md)
- [Automatic import observation](uitoolkit-import-observation.md)
- [Interaction structure](ui-interaction-structure.md)
- [Runtime player experience](ui-player-experience.md)
- [Projected marker coverage and clustering](projected-markers.md)
- [UI Builder preview](ui-builder-canvas.md)
- [Transition source review](ui-transition-source-review.md)
- [Style reference coherence](ui-style-reference-review.md)
- [Source editing](uss-editing.md)
- [Existing uGUI preview rebuild](ugui-rebuild.md)
- [Migration and dependency ownership](migration.md)

Executable command names, schemas and effects come from the installed native
catalog. Editor contract data lives beside the route provider; runtime semantic
and geometry contracts live in the Runtime types. The test assemblies are the
fixture authority, rather than a separately maintained command or test list.

## Peer layout and transition witnesses

`ui-audit-equal-width` compares visible flow children in the real native content container. `LayoutPeerComparisons` records coverage. `ui-audit-column`/`ui-audit-row` measure actual text-line separation. These checks do not judge icon artistry.

`VmUiTransitionAudit.Review` accepts3..600 distinct increasing natural-frame witnesses. The host must publish its actual destination predicate in the input frame, then sample real positions through at least two settled frames. Supply an explicit settlement budget and physical-pixel tolerance; the review rejects delayed publication, position snaps, empty motion coverage and unsettled/late movement. It does not fabricate or drive input, capture images, or infer business destinations. Runtime/source/pixel coverage remain separate.
