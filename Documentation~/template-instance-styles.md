# Template instance style ownership

The authoring usage index counts a template child once in its source file. That
does not make its state styles inlineable when named instances of that template
need different values. Unity's [Reuse UXML files manual](https://docs.unity.com/en-us/engine/6000.6/manual/uitoolkits/uielements/uie-structure-ui/uie-uxml/uie-reuse-uxml-files)
explicitly requires external selectors for template-instance style overrides;
AttributeOverrides cannot override inline style.

The failing control is one child with `display:none` in a shared template and
`#ExpandedInstance #Child { display:flex; }` in its importing host. Ordinary
single-consumer ownership advice incorrectly demands moving the base display
inline, which defeats the real state. Renaming class selectors to IDs preserves
the failure and is not a repair.

Recognize only proven direct template-instance contracts: an indexed host imports
the stylesheet, has multiple instances of the same indexed template, and a
two-part descendant selector selects a proper subset of those instances and one
unique child in that source. Only properties explicitly varied by those scoped
selectors remain external. Ordinary geometry, selectors without a real template,
unknown/nested source provenance, same-scope siblings and all-instance anchors
retain the existing ownership checks. Redundancy and native feedback checks are
independent and remain enabled.

The lifetime is one audit invocation. Index document/instance/target identities
once, then join scoped selectors to those products. Static cost bound: 262,144
total visits across elements, anchor registrations, source membership tests,
selector/property registrations and queries; selector length at most4,096.
Exceeding either bound throws an attributed incomplete-review error. No asset
rescan, native panel, generated fixture asset, global cache or recursive template
expansion is introduced. Strings are borrowed from the existing parse products;
temporary indices retain at most the work bound's entries (under32 MiB of index
storage excluding input strings and runtime dictionary overhead). Regression
controls cover ID/class targets, invariant geometry, non-template ancestors,
missing siblings, all-instance anchors and mismatched source targets. The public
USS self-test and original consumer request must pass after remote adoption.
