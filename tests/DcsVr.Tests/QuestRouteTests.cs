using DcsVr.Core;

internal static class QuestRouteTests
{
    public static void Register(Action<string, Action> test, string root)
    {
        void Check(bool ok) { if (!ok) throw new Exception("Quest route assertion failed"); }
        string Make(string name, string text)
        {
            var path = Path.Combine(root, "quest", name); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text); return path;
        }
        test("Quest preset and fixed foveation configuration", () =>
        {
            var p = OpenXrRoutes.Quest3;
            Check(!ProfileValidation.Validate(p).Any(i => i.Severity == IssueSeverity.Error));
            Check(JsonData.Deserialize<VrProfile>(JsonData.Serialize(p)) == p);
            Check(p.HeadsetRefreshHz == 72 && FramePacing.Describe(p).RequiredRenderedFps == 36);
            Check(!p.NeuralRendering && !p.PupilShift && !p.RefreshFromHeadset && p.DisableDcsVSync);
            var quad = ConfigurationWriters.QuadViews(p);
            foreach (var line in new[] { "force_no_eye_tracking=1", "horizontal_fixed_section=0.5", "vertical_fixed_section=0.45", "focus_multiplier=1.1", "peripheral_multiplier=0.4" }) Check(quad.Contains(line));
            var ofxr = ConfigurationWriters.Ofxr(p);
            foreach (var line in new[] { "backend=nvidia", "nvidia_preset=medium", "nvidia_input_scale=50", "triple_frame_gen=0", "adaptive_frame_gen=0", "d3d11_bridge=1" }) Check(ofxr.Contains(line));
            Check(HeadsetRefresh.Detect(RuntimeKind.Vdxr) is null);
            Check(!PimaxFovea.UsesPimaxPlay(p with { FoveaSource = FoveaSource.PimaxPlay }));
        });
        test("Quest rejects unsupported gaze, Pimax focus and foreign runtime overrides", () =>
        {
            var invalid = OpenXrRoutes.Quest3 with { Gaze = GazeMode.EyeTracked, PupilShift = true, FoveaSource = FoveaSource.PimaxPlay, QuadViews = QuadProvider.PimaxNative, RuntimeManifestPath = "steamxr_win64.json" };
            var codes = ProfileValidation.Validate(invalid).Select(i => i.Code).ToArray();
            foreach (var code in new[] { "quest-gaze", "quest-pupil", "quest-focus", "quad-runtime", "vdxr-manifest" }) Check(codes.Contains(code));
            Check(ProfileValidation.ResolveFeatures(OpenXrRoutes.Quest3 with { QuadViews = QuadProvider.PimaxNative }).QuadViews == QuadProvider.QuadViewsFoveated);
            Check(!ProfileValidation.Validate(invalid with { Desktop = true }).Any(i => i.Code.StartsWith("quest-") || i.Code == "vdxr-manifest"));
        });
        test("VDXR discovery supports registered custom installs without SteamVR fallback", () =>
        {
            var manifest = Make("custom/virtualdesktop-openxr.json", "{}");
            var missing = Path.Combine(root, "no-program-files");
            Check(WindowsInventory.VdxrRuntimeManifest(manifest, [], missing) == manifest);
            Check(WindowsInventory.VdxrRuntimeManifest(null, [manifest], missing) == manifest);
            Check(WindowsInventory.VdxrRuntimeManifest(null, [Make("custom/virtualdesktop-openxr-32.json", "{}")], missing) is null);
            Check(OpenXrRoutes.Manifest(OpenXrRoutes.Quest3, new() { PimaxRuntime = manifest, SteamVrRuntime = manifest }) is null);
            Check(ProfileValidation.Validate(OpenXrRoutes.Quest3, new()).Any(i => i.Code == "vdxr-not-found"));
        });
        test("VDXR detection proposes Quest without claiming a live USB connection", () =>
        {
            var manifest = Make("detect/virtualdesktop-openxr.json", "{}");
            var sources = new DetectionSources { ProcessRunning = n => n == "VirtualDesktop.Streamer", NeuralRuntimeFolders = [], PimaxPlaySettings = Path.Combine(root, "absent") };
            var result = SetupDetection.Detect(new(), new() { ActiveRuntime = manifest, VdxrRuntime = manifest }, sources);
            Check(result.Profile.Runtime == RuntimeKind.Vdxr && result.Profile.Gaze == GazeMode.Fixed && result.Profile.HeadsetRefreshHz == 72);
            Check(result.Active.Route is null && result.Notes.Any(n => n.Contains("USB")));
            Check(SetupDetection.Detect(new(), new(), sources).Profile.Runtime == RuntimeKind.Pimax);
        });
        test("Quest deployment pins VDXR, orders Quad Views before OFXR and restores originals", () =>
        {
            var exe = Make("deploy/bin/DCS.exe", "fixture");
            const string lua = "options = { graphics = { sync = true, Upscaling = 'DLAA' }, VR = {} }";
            var options = Make("deploy/Config/options.lua", lua);
            var runtime = Make("deploy/runtime/virtualdesktop-openxr.json", "{\"runtime\":{\"library_path\":\"runtime.dll\"}}");
            Make("deploy/runtime/runtime.dll", "fixture");
            var quad = Make("deploy/qv/qv.dll", "fixture");
            Make("deploy/qv/quad.json", ConfigurationWriters.LayerManifest("XR_APILAYER_MBUCCHIA_quad_views_foveated", quad, 1, "fixture"));
            Make("deploy/qv/settings.cfg", "# fixture");
            var ofxr = Make("deploy/ofxr.dll", "fixture");
            var inventory = new InventorySnapshot { DcsExecutable = exe, OptionsPath = options, VdxrRuntime = runtime, SteamVrRuntime = "wrong", PimaxRuntime = "wrong" };
            var plan = new DeploymentPlanner(new(null, null, null, Path.GetDirectoryName(quad), OfxrLayerDll: ofxr)).Build(OpenXrRoutes.Quest3, inventory, Path.Combine(root, "quest/managed"));
            Check(plan.LaunchEnvironment["XR_RUNTIME_JSON"] == Path.GetFullPath(runtime));
            Check(plan.LaunchEnvironment["XR_ENABLE_API_LAYERS"] == "XR_APILAYER_MBUCCHIA_quad_views_foveated;XR_APILAYER_XRFrameBridge_diagnostic");
            Check(!plan.Files.Any(f => f.Path.Contains("Cheeky") || f.Path.Contains("pupil-shift")));
            var store = new OriginalsStore(Path.Combine(root, "quest/originals")); store.Apply(plan);
            var applied = new LuaOptions(File.ReadAllText(options));
            Check(applied.Get("graphics", "sync") == "false" && applied.Get("VR", "openxr_eyeGaze") == "false");
            Check(applied.Get("graphics", "Upscaling") == "'DLAA'");
            Check(store.RestoreOriginals().Complete && File.ReadAllText(options) == lua);
        });
    }
}
