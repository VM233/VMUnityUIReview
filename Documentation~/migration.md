# Adopting the separate UI review package

Automation 0.7.0 removes UI review implementations and their executable metadata.
Install this package alongside that revision in the same manifest transaction,
then resolve both immutable Git pins, import, reload and compile. An Automation
installation without this optional package does not advertise UI review routes.

The existing UI review command names and C# type namespaces remain unchanged.
Their assembly and owning package move to VMUnityUIReview. Consumers with an
explicit asmdef reference to the old runtime assembly for UI audit types must
reference `VMUnityUIReview.Runtime`; Editor integrations use `VMUnityUIReview.Editor`.
Moved source `.meta` GUIDs are preserved. No forwarding implementation or old
review registry remains in Automation.

The persisted project configuration remains
`ProjectSettings/VMUnityAutomationUIToolkitAudit.json`; its sole owner is now
this package. This avoids an unrelated configuration rename or parallel files.
Absent automatic-audit settings enable USS and UXML import checks; explicit
settings are reflected by each command's automatic-audit status. Feedback errors
cannot be suppressed by the existing style-ownership suppression markers.

Automation retains generic element queries, pointer/keyboard input, screenshots,
window/document lifecycle bindings, source paths, catalog execution and jobs.
This package supplies optional provider metadata and consumes those services in
one direction. The official Pipeline reads the package identity from that metadata.

Native authored Button feedback coverage applies to imported template hosts and
the unique PanelSettings theme in the declared roots. Runtime-created controls,
custom non-Button click handlers, uGUI Selectable feedback and device presentation
need their corresponding native/runtime evidence. Their absence from this
authoring report is never an acceptance claim.
