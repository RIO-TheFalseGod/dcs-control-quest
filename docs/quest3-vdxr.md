# Quest 3 with Virtual Desktop / VDXR

Select **Quest 3 · VDXR** in the route picker or Guided Setup. Fresh app installations start with this route; existing saved drafts and applied profiles remain intact. Selecting Quest from another route loads the starting values below. Once on Quest, your edits remain editable and saved.

The intended rendering path is DCS → bundled Quad Views (fixed foveation) → optional DCS DLSS/DLAA → OFXR Bridge → VDXR → Virtual Desktop over USB → Quest 3. DLSS/DLAA is game image processing, not an additional OpenXR layer. DCS's existing upscaling choice is preserved initially; DLSS 5 stays off. If explicitly enabled later, DLSS 5 uses the existing Cheeky focus adapter and its separately supplied NVIDIA runtime.

| Setting | Initial value |
| --- | --- |
| Refresh | 72 Hz, set manually in Virtual Desktop |
| OFXR | NVIDIA, fixed 2× (36 rendered → 72 output FPS target) |
| Optical flow | Medium, 50% |
| Fixed focus width × height | 0.50 × 0.45 of the view |
| Focus / peripheral multipliers | 1.10 / 0.40 |
| Eye tracking / pupil shift | Off |
| DCS desktop VSync | Off |
| DCS FPS limit | 300, leaving pacing to OFXR |

The 36 FPS figure is a rendering target, not an extra external limiter. OFXR follows the actual runtime cadence; match Virtual Desktop's rate to the profile. The app does not change Virtual Desktop's refresh rate, transport or SSW settings.

## Runtime discovery and launch

The normal 64-bit manifest is `C:\Program Files\Virtual Desktop Streamer\OpenXR\virtualdesktop-openxr.json`. Detection also checks the active OpenXR runtime and registered AvailableRuntimes for a custom install. A missing VDXR installation never falls back to Pimax or SteamVR. Under DCS & headset → Runtime override, select the same 64-bit manifest filename in a custom installation if needed; the `-32` manifest is rejected.

Readiness checks parse the manifest and verify its runtime library is x64. Launch pins the selected manifest with `XR_RUNTIME_JSON` for the DCS process; it does not change the system's OpenXR runtime. Bundled Quad Views loads before OFXR. Quest focus values and refresh never come from Pimax Play or SteamVR. Quest has no Sboys driver requirement.

Detect my setup proposes Quest when VDXR is the registered active runtime and Virtual Desktop Streamer is running. This is evidence of software configuration, not a verified headset session. Streamer alone cannot establish the connected headset, USB transport or refresh rate.

## First flight

1. In Virtual Desktop select VDXR, connect the Quest 3 over USB, and confirm that USB is the active connection. Use matching Virtual Desktop versions that support your USB setup.
2. Set 72 Hz and disable Virtual Desktop SSW while using OFXR.
3. Select Quest 3 · VDXR and check the profile before launch. Start with DLSS 5 off.
4. In a simple mission, check both eyes, cockpit text, head motion and focus boundaries. Verify OFXR activity and actual cadence before adjusting resolution or optional DLSS/DLAA.
5. Use **Back to stock DCS** to restore the app's owned changes.

The managed-code tests verify configuration, layer order, runtime isolation and restoration with fixtures. They do not establish in-headset compatibility or achieved performance. Native Quad Views/OFXR binaries still need the normal release build process.

References: [VDXR implementation and documentation](https://github.com/mbucchia/VirtualDesktop-OpenXR), [Virtual Desktop downloads and support](https://www.vrdesktop.net/).
