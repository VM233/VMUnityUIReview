# Reviewed design composition

`VmUiAuditExpectation.Group` requires its actual element to remain within the
declared task/navigation group. `TextContent` declares the meaningful text elements
in one reviewed region; any additional visible nonempty text fails. These contracts
join `VmUiExperienceAudit.Review`, report findings under `design-action-group` and
`design-unclassified-text`, and contribute to `InformationCompositionChecks`.

The game owner supplies fresh actual elements after deciding what information is
necessary. Do not generate the allowed set by enumerating the current text tree:
that would admit the very redundancy being checked. Normal visibility/eligibility
expectations still own required content and action availability. Use the relevant
task group: a menu exit can be secondary during play and primary after a session.
The audit does not infer frequency, redundant meaning or attractiveness.

Static cost ledger: selected inventory remains <=4096 nodes and64 ancestry levels;
<=1024 group checks add <=65536 ancestry visits. Declared text entries and traversed
region nodes share a hard8192-visit invocation budget, checked with the existing
500ms deadline. Stack/set state is local, <=4096 nodes plus8192 allowed entries
(<1MiB); no asset loads, repeated source scans, workers or retained caches. Budget
exhaustion fails rather than publishing partial acceptance. Native matched tests
cover redundant captions, hidden/reappearing content, wrong/correct groups and
duplicate declarations. Actual input, localized pixels and visual design remain
independent acceptance layers.
