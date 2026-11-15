# Content and response contracts

The existing experience audit rejects a flat text field containing five or more
nonempty explicit lines. Native wrapping is not an authored semantic boundary.
An explicit parallel bullet/number list or rich-text subheadings remain valid;
the gate cannot judge whether the facts were worth showing. Manual pixel and
semantic review remain required. The same pure check is available to authoring
and localization consumers; vague action rejections are checked at their source.

`VmUiTaskResponseAudit` reviews actual rendered frames after a real input. The
caller supplies the native frame/time, authoritative destination publication and
visible destination fraction, including any retained source-image occlusion.
Publication, first visible response and full settlement have separate budgets.
The clock starts at input, not layout/publication. Empty, nonfinite, unordered or
over-budget input fails admission. A logical selected flag alone is insufficient.

Repeated return witnesses identify the task context, action, declared destination
and actual destination. Review rejects wrong destinations and variation between
equivalent repetitions. The host must inspect the actual parent task and painted
state; no string comparison can discover those facts by itself.

The W3C [tabs pattern](https://www.w3.org/WAI/ARIA/apg/patterns/tabs/) ties active
selection to its actual panel. Nielsen's [response-time guidance](https://www.nngroup.com/articles/response-times-3-important-limits/)
supports a 100ms immediate-response budget; this is a caller-owned UX target,
separate from the product's intentional animation duration. The package does not
silently impose a time limit on every navigation or loading operation.

## Static cost ledger

Glyph pen review reads the real mesh vertices, pigments, triangle-pair boundary
and canvas normalization. It measures parallel strip edges, then compares the
normalized widths against the caller's explicit family pen. It rejects empty
coverage and thin/thick original controls. This does not judge icon meaning or
the artistic coherence of filled shapes.

Before implementation: text inputs contain at most 65,536 UTF-16 units. One
linear pass counts at most 65,537 line boundaries; a fixed set of ten vague
reason expressions uses ordinal string searches, with no regex/backtracking.
The native experience audit reuses its <=4,096 selected elements and <=500ms
budget; no extra tree inventory, readback or per-frame scanner is introduced.
Text composition adds no retained native resources. Localization consumes the
same helper once per already bounded table value, on its existing worker/step.

Response input contains 2..600 distinct increasing frames; return input 2..128
visits with <=512-unit identities. Each review is O(n), with <=600 numeric
observations and <=128 dictionary entries (<256KiB managed scratch). There is no
native query, capture, clock override or task scheduling inside these reviewers.
Glyph capture admits <=4,096 vertices/24,576 triangle indices per mesh and <=128
glyphs. Each triangle pair does <=36 edge comparisons; no pairwise search across
glyphs. One mesh's detached arrays and <=4,096 measured widths are retained at
a time by a caller that batches capture/review. Native pigment and pen admission
must precede artistic acceptance. The existing host keeps its time budget.
Matched controls retain delayed publication, hidden-but-published destinations,
late onset, stale launcher and alternating return, alongside prompt natural
motion and repeated stable destinations. Verdict: pass for these frozen bounds.
