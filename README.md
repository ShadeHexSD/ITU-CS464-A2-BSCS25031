# CS464 Assignment 02 · Tayyab Ahmed Minhas · BSCS25031

- **Video:** [(https://youtu.be/RweQYPqX5is)]
- **Input:** Unity Input System (`InputSystem.actions`, Move and Jump). I read input in `Update()` and move the Rigidbody in `FixedUpdate()`.
- **Ground check:** `Physics.CheckSphere` at the Feet child, radius 0.19 m, and the mask only includes the Ground layer. A wire-sphere gizmo shows it.
- **Camera:** `CameraFollow.cs` runs in `LateUpdate()` with `1 - e^(-sharpness × deltaTime)` smoothing, plus look-ahead based on the player's velocity. The player walks relative to the camera's flattened forward and right.
- **Juice:** A landing squash on the Body mesh. It starts only after a real landing (more than 0.10 s in the air) and eases back to normal with `Ease.OutBack`. The collider never changes size.

## Tuning

| Setting | Value | Why this number |
|---|---|---|
| Speed | 5 m/s | Lecture 04's starting speed, and it felt fine. It also means the 0.10 s coyote window gives you 0.5 m past a ledge. |
| Jump height | 1.8 m | I set the jump in metres so it's easy to reason about, instead of guessing a force. |
| Time to apex | 0.606 s | Follows from 1.8 m and gravity 9.81. Unity's fixed steps make the real number a touch different. |
| Fall multiplier | 2.2× | Extra gravity only on the way down, so the jump peaks at the same height but you land sooner and it feels less floaty. |
| Coyote time | 0.10 s | Lets you still jump just after running off an edge. At 5 m/s that's up to 0.5 m. |
| Jump buffer | 0.12 s | If you press jump a little too early before landing, it still counts instead of getting lost. |
| Camera smoothing | sharpness 5 | Same follow feel at any frame rate, unlike a constant Lerp factor. Used in every level. |
| Look-ahead | L1 0.35 s, L2 0.15 s, L3 0.15 s, L4 0.10 s | The camera aims slightly ahead of where you're going. Big open maze gets more (1.75 m at 5 m/s), tighter levels get less so the camera doesn't swing around (0.75 m, 0.75 m, 0.5 m). |

## Four answers

1. **The jump, in numbers.** With a 1.8 m jump and gravity 9.81, launch speed is √(2 × 9.81 × 1.8) ≈ 5.94 m/s. With a 1 kg Rigidbody, that's an impulse of about 5.94 N·s. It takes about 0.606 s to reach the top. Falling back down with the 2.2× multiplier takes about 0.408 s. So one jump at 5 m/s carries me roughly 5 × (0.606 + 0.408) ≈ 5.07 m on flat ground. That's well past the 3 m gaps from Lecture 03, so it clears them comfortably.

2. **Update and FixedUpdate.** In `Update()` I read Move and Jump, work out the camera-relative direction, check the ground, track the coyote and buffer timers, and queue a jump. In `FixedUpdate()` I set the horizontal velocity (keeping Y), fire the queued jump once, and add the extra fall gravity. If I read the jump press in `FixedUpdate()` I'd miss presses, because a button press only lasts one frame and not every frame has a physics step. If I moved the physics into `Update()`, movement would depend on frame rate. Also, velocity is already in m/s, so I don't multiply it by `deltaTime`.

3. **Vector3.Dot.** My ground check uses `CheckSphere`, so I don't use Dot right now. If I switched to contact normals I'd write `float d = Vector3.Dot(contact.normal, Vector3.up); bool ground = d > 0.5f;`. Near +1 means a flat floor, 0 means a wall, and negative means something facing down like a ceiling. The 0.5 threshold stops walls from counting as ground.

4. **The juice.** It's a landing squash using `Ease.OutBack` with `Vector3.LerpUnclamped`, so it overshoots a little before settling. It lasts 0.18 s, which is about 11 frames at 60 fps (10.8). Short enough to not get in the way, long enough to actually see. The Body squashes to 0.70 on Y and about 1.195 on X and Z (1/√0.70) so it keeps roughly the same volume, then snaps exactly back to its original scale. Only the mesh changes, never the Rigidbody or collider.

## Levels

| Level | What the controller must do there | What I changed for this jump |
|---|---|---|
| Level01 | Get through the coin-and-key maze without catching on walls and reach the exit. | Same 5 m/s and 1.8 m jump. Camera offset (0, 20, -11) and look-ahead 0.35 s so you can see more of the maze. |
| Level02 | Weave around the shooting-range targets and barriers to the goal. | Same Player prefab. Camera offset (0, 8, -11) and look-ahead 0.15 s so it doesn't swing when you change direction. |
| Level03 | Handle the football, switches, doors and platform areas. | Same jump. Camera offset (0, 7, -6) and look-ahead 0.15 s for closer navigation. |
| Level04 | Get past cover and up the stairs to the spike-planting area. | Same Player prefab. Camera offset (0, 6, 5) and look-ahead 0.10 s for the tighter layout. |
