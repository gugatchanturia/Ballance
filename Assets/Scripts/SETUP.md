# Ballance - Interstellar-style black hole VFX test

The `BallanceBlackHoleLens.cs` script aligns an MIT-licensed, Unity 6 URP
black-hole raymarching VFX prefab with the real curved gravity funnel in
`GravityPhisicsTest`. It is **visual only**. It deliberately does not replace
or modify `BlackHole.cs`, `GravityGrid.cs`, `CurvedFieldPhysicsTest.cs`, the
field's Rigidbody, or Earth.

## 1. Preserve the working physics scene

Save and commit your working project. Duplicate `GravityPhisicsTest` to a new
scene, e.g. `GravityLensTest`, before testing this integration.

## 2. Bring in the existing Unity 6 shader

Source: https://github.com/tantaneity/urp-black-hole

The repository is MIT-licensed. Download it separately, then copy **only** its
`Assets/BlackHole/` directory AND its `Assets/BlackHole.meta` into your own
Unity project's `Assets/` directory. Keep the `.meta` files so the prefab,
material and shader stay linked. Do **not** copy the other project's Packages,
ProjectSettings, or unrelated demo assets over your Ballance project.

Copy that repository's `LICENSE` to your own project's
`ThirdPartyLicenses/urp-black-hole-LICENSE.txt` and credit the shader author
in your project README / course submission where appropriate. Confirm your
assignment permits third-party MIT-licensed visual effects.

## 3. Install the Ballance-specific adapter

Copy `BallanceBlackHoleLens.cs` to your project's `Assets/Scripts` folder.

## 4. Set up the new scene

- In `GravityLensTest`, disable the old **BlackHoleAccretion** component on the
  existing physical `BlackHole` object. Keep its `BlackHole` and Grid physics
  untouched. The existing `BlackHoleVisual` MeshRenderer should stay hidden.
- Drag `Assets/BlackHole/Prefabs/BlackHole.prefab` into the SCENE ROOT (not
  under the non-uniformly scaled GravityField or physical BlackHole).
- Rename this prefab instance `LensedBlackHoleVisual` to distinguish it from
  your gameplay BlackHole.
- Add **BallanceBlackHoleLens** to the imported visual prefab root.
- Drag your existing gameplay `BlackHole` into the **Physical Black Hole** slot.
- Drag the existing `GravityField/GravityGrid` into the **Gravity Grid** slot.
- Leave the script's initial dimensions at Visual Diameter 4.5, Horizon Scale
  0.18, Disk Inner 1.5, Disk Outer 4, Raymarch Steps 128, Supersampling OFF.
- The adapter automatically replaces the prefab's original scale of 30 with
  the gameplay-appropriate scale and places the lens near the well's bottom.

## 5. Optional bloom AFTER basic rendering works

The upstream shader has HDR-bright emission. To get cinematic glow, add a
Global Volume with Bloom, enable Post Processing on Main Camera, and tune the
Bloom threshold/intensity. ACES Tonemapping is recommended by the author.
Don't change your active URP renderer; it is already 3D-compatible.

## 6. What this version does and does not do

- YES: physically bent light rays through a Schwarzschild-style numerical
  approximation, photon shadow, warped upper/lower accretion disk, animated
  orbiting structure and lensed PROCEDURAL stars within the prefab's sphere.
- YES: VFX tracks a curved funnel as the gravity field tilts.
- NO: it does not lens your actual rendered cyan/amber grid or the actual
  camera scene. The source shader explicitly lenses its own procedural sky;
  that is the one tradeoff in this first realistic VFX integration.
- NO: no change in black-hole physics, collision, grid deformation or Earth.
- EXPECT: the visual sphere replaces pixels within its bounded screen area
  with a procedural sky, so it can look like a circular cosmic region over the
  grid at first. Evaluate that appearance before adding another effect.

## Troubleshooting

- `Pink`: shader import/URP issue; inspect console, ensure all files under
  `Assets/BlackHole/Shaders` and their `.meta` files were copied.
- `Missing material`: check that the original prefab references its BlackHole
  material, and that the shader exists.
- `Black hole too large`: lower **Visual Diameter** from 4.5 to ~3.5. For a
  consistent physical shadow size, increase Event Horizon Scale toward 0.2
  but note the shader caps it at 0.2.
- `Black hole appears too deep`: increase Height Above Well from 0.2 to 0.5.
- `Disk looks too face-on`: change Disk Tilt Degrees by ~10-20 degrees.
- `Slow frame rate`: reduce Raymarch Steps to 96, leave Supersampling OFF.
- `Only stars, no disk`: verify inner radius 1.5, outer radius 4, disk
  brightness 6, and prefab material uses shader `BlackHole/Gargantua`.

## Visual size note

The imported sphere's mesh diameter is 1. At a Visual Diameter of 4.5 and
Horizon Scale 0.18, its theoretical event-horizon radius is about 0.405
world units (0.5 * 4.5 * 0.18). The disk begins around radius 0.61 and
extends to radius 1.62. This is intentionally comparable to Ballance's
physical capture radius 0.45 and gravity influence radius 3.

## Test checkpoint

Play, tilt the field, and see whether the effect's center stays over the
physical hole. Check that Earth still falls into the original funnel. Then
send a screenshot; only adjust visual geometry/brightness if necessary.
