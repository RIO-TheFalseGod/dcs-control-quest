namespace DcsVr.Core;

/// <summary>Explicit runtime selection; a missing route never falls back to another headset.</summary>
public static class OpenXrRoutes
{
    public static string Name(RuntimeKind route) => route switch
    {
        RuntimeKind.Pimax => "Pimax OpenXR",
        RuntimeKind.SboysSteamVr => "Sboys through SteamVR",
        RuntimeKind.Vdxr => "Quest 3 · Virtual Desktop (VDXR)",
        _ => "Unknown runtime"
    };

    public static string? Manifest(VrProfile profile, InventorySnapshot inventory) =>
        !string.IsNullOrWhiteSpace(profile.RuntimeManifestPath) ? profile.RuntimeManifestPath : profile.Runtime switch
        {
            RuntimeKind.Pimax => inventory.PimaxRuntime,
            RuntimeKind.SboysSteamVr => inventory.SteamVrRuntime,
            RuntimeKind.Vdxr => inventory.VdxrRuntime,
            _ => null
        };

    public static bool IsVdxrManifest(string? path) => !string.IsNullOrWhiteSpace(path) &&
        Path.GetFileName(path).Equals("virtualdesktop-openxr.json", StringComparison.OrdinalIgnoreCase);

    public static VrProfile Quest3 { get; } = new()
    {
        Id = "quest3-vdxr", Name = "Quest 3 · VDXR + Quad Views + OFXR 2x",
        Runtime = RuntimeKind.Vdxr, QuadViews = QuadProvider.QuadViewsFoveated,
        Gaze = GazeMode.Fixed, Tracking = TrackingKind.Slam, FoveaSource = FoveaSource.Profile,
        FoveaWidth = .50, FoveaHeight = .45, QuadFocusScale = 1.10, PeripheralScale = .40,
        HeadsetRefreshHz = 72, RefreshFromHeadset = false, FrameGen = FrameGeneration.Nvidia,
        FrameGenFactor = 2, FpsLimit = FpsLimitMode.RuntimeHeadroom, RenderedFpsCap = 36, NvidiaFlowScale = 50, FlowPreset = NvidiaFlowPreset.Medium,
        DisableDcsVSync = true, PupilShift = false, NeuralRendering = false, FoveatedDlss = false
    };
}
