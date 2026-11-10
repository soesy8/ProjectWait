# Wait regression tests

Unity 6000.3.18f1 / Test Framework 1.6.0. No runtime asmdef migration, scene/manifest edits, builds or device dependency.

## Run

1. Save or close modified/unsaved scenes. Exit Play Mode and wait for compilation.
2. **Tools > Wait > Tests > Run all regressions** runs EditMode, then PlayMode in the Editor.
3. Read `Assets/M/CodexTemp/RegressionResults~/<UTC-run-id>/summary.json` and the per-mode `EditMode.xml/.json`, `PlayMode.xml/.json` files. Failures do not suppress the other phase. A zero-test, skipped, count-mismatch, aborted or infrastructure-error run is not a pass.
4. **Tools > Wait > Tests > Cancel regressions and restore editor** cancels the owned UTF job and restores after UTF cleanup. The standard Test Runner's Stop also produces nonpassing/cancelled results.

The same assemblies appear automatically in **Window > General > Test Runner**, under EditMode / PlayMode. Filter category `WaitRegression`. The menu runner is recommended for the serialized Editor-state snapshot and combined report. `ProjectWait.M.EditorTests.WaitRegressionRunner.RunAll()` is the public static entry point when invoking from an already open Editor; it is asynchronous and must not be followed immediately by Editor quit.

## Coverage / isolation

- Real reflected `Assembly-CSharp` Flow: intercom and unanswered/119 routes, explicit identity report, readable untimed directions, safety gate and reset; negative/NaN/infinite delta stability.
- Both experience variants cover order-independent boarding: the first card and tutorial show 1F selection plus ball receipt together, either real-object order works (front/low floor panels), ownership updates immediately, completed input is idempotent, and the next scene waits for both tasks. The remaining task stays untimed and restart clears progress.
- Door safety in both variants: rescue advice replaces fear with a small relieved smile; an in-place sigh precedes the approach, the knees soften without a collapse, the friend requests a short rest rather than comfort from fear, and the safe-wall/restart poses release the temporary bend. Assertions run the real pose writers in their LateUpdate order and inspect final blendshape weights and joints; legacy optional panic meshes are not assumed assigned.
- EditMode seeds the FPS ownership preconditions via reflection (`OnEnable` intentionally refuses EditMode), then invokes real `ApplyLimit`/`OnDisable` on preview-scene objects: older/newer release order, distinct owner values and exact global baseline restoration. Temporary static bookkeeping is restored. Actual FPS acquisition/release lifecycle is also covered in PlayMode.
- `ExperienceRegressionTests` loads the main experience scene additively by exact asset path. Exercises actual observation activation and actual UI button `onClick` callbacks along both complete routes, identity description text after spatial presentation, continuous hover through hint revisions, pause/resume and the pause-menu restart button.
- `MovementAndUIRegressionTests` additionally loads both the main experience and isolated DialoguePreview scene: offset camera/capsule teleport destinations (including heading reset), opening teleport idempotence and missed arrival, long-frame boarding/exit/home arrival, outgoing world card/speaker/button corners through layout changes, correct local pose after reopen/restart, and desktop mouse instructions/icons. Offset tracking is simulated through the real camera/rig transforms; this is not physical HMD validation.
- Wear-aware stable height policy, explicit confirmation without presence support, rewear, invalid tracking; actual pause-menu calibration callback and camera/capsule floor alignment; long-frame first-floor arrival and usable exit.
- Paused cinematics continue height observations; clean unsaved scenes are refused before snapshots; the shutdown handler restores persistent Enter Play Mode options and records interrupted runs without restoring/saving scenes during shutdown.
- Real PlayMode pipeline-owner and FPS-owner lifecycle, releasing owners in either order. The installed NUnit does not implement `NonParallelizable`; these tests rely on Unity Test Runner's sequential execution, without that unsupported attribute.
- Test-only helper/PlayMode assemblies use `UNITY_EDITOR` constraints plus guarded source and `autoReferenced: false`; NUnit test assemblies additionally use `TestAssemblies`. No test assembly is compiled into a Player. PlayMode intentionally cannot use `includePlatforms: ["Editor"]`: UTF 1.6.0 classifies that as EditMode, as confirmed in its installed `EditorLoadedTestAssemblyProvider`.
- All transient objects, preview/loaded scenes and globals are cleaned in fixture teardown / `finally`. Existing scene roots are suspended then reactivated, never destroyed/saved. The menu snapshots scene setup, selection, quality pipeline, FPS/VSync, time/audio/cursor/background, fast-enter-play options and Editor pause state across domain reloads. Dirty scene protection refuses to start rather than autosaving/discarding work; if new dirty work blocks restoration it reports a restoration error.

## Limits

These are deterministic engine/UI regressions, not real-time pacing, physical headset comfort checks or a Player build. Separate harnesses in `CodexTemp/SingleSceneTools~/` cover virtual trigger events and head collision; `CodexTemp/RemainingFixesTools~/` includes actual D3D11 Coverage/Composite depth and occlusion readback (perspective + orthographic). Synthetic silhouette seeds isolate the Composite conversion; this is not full JFA/stereo renderer validation.

Unity Test Runner itself temporarily creates `Assets/InitTestScene*.unity`, then removes it; the user approved this specific out-of-M automatic temporary exception. Do not suspend roots in this UTF bootstrap scene. No test scene is retained or added to Build Settings.
