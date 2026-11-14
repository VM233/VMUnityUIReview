# Projected marker coverage

Use this review for map/location markers and comparable projected UI. The game
owns source eligibility, geographic anchors, intended normalized glyph sizes, collision sizes and
cluster membership. `IVmUiProjectedMarkerSource` captures those facts separately
from the active native renderer/mesh publication. Do not derive an expectation
from the displayed size or omit a source because its renderer is hidden.

The Player-compatible `VmUiProjectedMarkerAudit` reports missing/inactive glyphs,
size drift, displaced anchors/mesh centres, wrong counts, unknown or duplicate
members and clusters whose members do not directly overlap the representative's
source footprint plus the source-owned collision padding. It rejects zero
required coverage. A representative keeps its actual geographic anchor; an
unrestricted nearest assignment or transitive chain cannot qualify distant
members. Panning and zooming require fresh projections from the same camera.

`CollisionSizePixels` declares the actual input/layout footprint independently
of `ExpectedGlyphSizePixels`. A narrow glyph may have a square touch target;
that target controls direct-overlap membership without excusing glyph-size drift.
Both sizes are mandatory positive pixel pairs, supplied from authored policy.

Pixel points/sizes use two finite numbers and rectangles use four, in one actual
screen-pixel coordinate system. `VmUiProjectedMarkerGeometry.ProjectMeshBounds`
projects the eight corners of the actual MeshFilter bounds through its native
transform/camera. Source policy, silhouette visibility, transparency and
occlusion semantics remain the domain owner's responsibility. Passing bounding
geometry does not certify visible pixels or the quality of a map design.

Discover the typed `ui/review-projected-markers` contract through the installed
official catalog. Supply the exact active source component identity, immutable
source/build fingerprint, scenario and optional Locale context. The facade reads
one source; it does not search scenes, move the camera, inject input, create a
fixture or mutate world/save state. The result includes the captured snapshot,
native frame/build context and measured coverage. Manual visual review remains
required. Automation continues to own transport, input, capture and jobs.

## Bounds and acceptance

Static Cost Ledger: at most1024 candidates and1024 publications,4096 total member
references,1024 findings. Candidate/member identities and families are capped
at160 characters, owner256, source/scenario512 and Locale64. Dictionary/set
construction and one membership pass are O(N+M); there is no pairwise member
search or scene enumeration. Scratch/snapshot serialization is bounded by8MiB
for these DTO sizes. Each optional mesh projection performs exactly eight native
corner projections; the source must declare its own capture bound. The official
facade limits capture plus review to500ms, and refuses incomplete/over-budget
results rather than publishing a partial pass. No native resources or callbacks
outlive the synchronous call. Ledger: pass for these frozen axes.

The focused fixture type is `VmUiProjectedMarkerAuditTests` in the test assembly
declared by `Tests/Editor/VMUnityUIReview.Editor.Tests.asmdef`. It retains missing
same-family, parcel-shrunk, distant-nearest, displaced, inactive, stale identity,
count, membership and zero-coverage controls alongside complete and genuinely
overlapping cases. Consumer acceptance must replay its actual affected zooms,
source changes and native input; package fixtures do not certify that consumer.
