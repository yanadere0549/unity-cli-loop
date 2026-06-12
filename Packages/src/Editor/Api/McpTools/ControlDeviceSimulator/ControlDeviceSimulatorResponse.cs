#nullable enable

namespace io.github.hatayama.uLoopMCP
{
    public class ControlDeviceSimulatorResponse : BaseToolResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string Action { get; set; } = "";
        public bool IsWindowOpen { get; set; }
        public int? CurrentDeviceIndex { get; set; }
        public string? CurrentDeviceName { get; set; }
        public string[]? AllDeviceNames { get; set; }
        public int? CurrentRotation { get; set; }        // 0/90/180/270
        public int? CurrentScale { get; set; }           // 10-100
        public int? ScreenWidth { get; set; }            // ScreenSimulation.width
        public int? ScreenHeight { get; set; }           // ScreenSimulation.height
        public string? OrientationName { get; set; }     // ScreenSimulation.orientation.ToString()
    }
}
