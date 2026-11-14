# Functional work areas

The optional `rules.validateWorkContextOwnership` policy extends the existing
UXML layout audit and its automatic import owner. It compares actual serialized
Prefab producers implementing `IVmUiWorkContextSource` with their real UIDocument
and imported templates. A producer returns the unique element name from its
serialized binding and the functional context it actually owns.

Mark that element with `ui-structure: {"role":"activity","state":"context-id",
"owner":"Fully.Qualified.ComponentType"}`. It is a task scope and work context.
Each activity must be inside a real declared navigation panel, with actual tab
entries. Independent contexts cannot share the same panel. A Foldout, extra
labels, empty scopes or a generic actions marker do not establish categorization.
Require nonzero matched sources/bindings; inspect the source's actual behavior
when assigning its semantic context. This is a binding/structure contract, not
natural-language inference or proof of runtime callback effects.

## Static cost ledger and acceptance

Opt-in only. Per request admit128 UI Prefabs,128 unique dependent UXML documents,
32768 MonoBehaviour references,32768 XML nodes,512 bindings and8MiB XML text.
Read each Prefab/document once; index names and activity/panel ancestry once.
One native dependency call per UIDocument, at most128 calls; at most512 name
lookups and65536 bounded ancestor visits. Each producer-backed UIDocument is cloned once without attaching a panel, at most8192 native elements per tree, then cleared. No runtime instance, frame callback,
business initialization or save is created. Retained source/index/scratch budget
64MiB; a2s observation budget is checked between native calls. Excess input or
unavailable native producers fails completeness instead of accepting a subset.

Success requires the original combat/crafting/mining dump to fail, correctly
separate navigated pages to pass, and missing/wrong producers, fake unlinked
panels, duplicate names and work areas without controls to fail. Replay actual
Prefab integration and the complete localized UI after repair. A failed native
binding or budget assumption stops this method before broadening its limits.
