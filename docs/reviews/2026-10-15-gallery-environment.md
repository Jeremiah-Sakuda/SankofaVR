# Living gallery environment — October 15

The museum now has a complete architectural surround: a warm plaster exhibit recess, smoked-walnut side bays, honed stone slabs, a timber-ribbed mineral ceiling, bronze joints and recessed warm light strips. A quiet rear bench establishes scale when looking back. The canvas has a stepped walnut/bronze frame and a separate shadow gap; its title has breathing room above it. Gallery branding sits above the artwork and context remains to the left, with dark lettering against the lighter plaster.

This implements the scope’s beautiful, readable seated gallery under **Polish & Presentation** and **Experience Design**. Ownership stays in the editor presentation builder (`SankofaGalleryArchitecture`), generated materials/scene and one architectural shader. Story rules and actions are unchanged. The room is a contemporary theatrical setting, not a claimed reconstruction of Kenyan or Akan architecture.

The gallery remains one exhibit viewed from the existing seat. The ledge has a bronze underside and supports, while controls and reach anchors keep their previous positions. The surround belongs to the home presentation and disappears when the story opens. The canonical painting, separate keepsake and authored narration remain unchanged.

## Rendering tradeoff

The new material shader uses one opaque pass, existing project grain, object-attached detail, analytically shaded surfaces and filtered stone joints. There are no new texture downloads, realtime lights, reflection probes, post-processing effects or external libraries. More opaque geometry/materials and extra grain samples cost rendering work; lower complexity than a dynamically lit interior does not establish Quest performance. Unity’s single-pass stereo macros are retained. Device GPU timing, stereo comfort and actual-hand reach remain unverified.

## Verification

**306/306 core, 31/31 Unity editor and 7/7 final targeted runtime checks passed.** The latter cover both accepted story threads, Keep and modern continuation back to the unchanged painting; passive/reduced-motion camera stability; pointer slip/cancellation; seated height; settings/credits; room enclosure, text obstruction and withdrawal; and 108 left-context projection combinations. A later branding-only adjustment moves the wordmark above the painting and the purpose line into the context column; the final two presentation checks **passed**, extending projection and mutual-overlap coverage to all five wall text blocks. Their report is `artifacts/tests/gallery-environment-branding.xml`. This is a focused suite, not a repeat of all historical runtime checks.

Final muted desktop captures: [seated entry](../media/living-gallery.png), [side surround](../media/gallery-environment-side.png). Local `artifacts/living-gallery/environment-{front,left,right,rear,ceiling}.png` captures cover the surround. Runtime report: `artifacts/tests/gallery-environment-final.xml`; raw log: `artifacts/logs/gallery-environment-final.log`. Batch frame-rate logs are not device performance evidence.

The earlier two presentation tests passed: a complete surround, clear wall labels, withdrawal/return, and the existing 108 left-context projection combinations. That first visual review prompted a lighter ceiling and more space above the frame; its captures are development evidence, not the final comparison.

No sound playback, narration export, new generated art, cultural acceptance, headset installation or public upload occurs in this pass.

Muted Meta XR Simulator 207 review displayed the new architecture with the process-local runtime. Native aiming followed by trigger press/release selected **Enter painting**, and the opening caption and return control appeared. The initial right-side branding clipped at the mirror edge, prompting the final central wordmark/context-column layout. No global runtime setting was changed. This is simulator controller/display evidence, not real hands, binocular comfort or Quest performance.

A fresh final simulator launch confirmed the corrected wordmark, exhibit title and context fully visible in the forward mirror view. The editor was then closed for the review build. Final editor report: `artifacts/tests/gallery-environment-editor-final.xml` (31/31); final simulator log: `artifacts/logs/gallery-environment-simulator-final.log`.
