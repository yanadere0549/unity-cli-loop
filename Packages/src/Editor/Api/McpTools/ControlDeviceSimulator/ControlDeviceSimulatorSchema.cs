using System.ComponentModel;

namespace io.github.hatayama.uLoopMCP
{
    public enum DeviceSimulatorAction
    {
        GetState = 0,     // Report full state (device, orientation, scale, screen size)
        Open = 1,         // Open/bring-to-front the Simulator window
        ListDevices = 2,  // Report device list only
        SelectDevice = 3, // Switch to device by DeviceIndex or DeviceName substring
        SetRotation = 4,  // Set rotation (0/90/180/270 degrees)
        SetScale = 5      // Set scale (10-100)
    }

    public class ControlDeviceSimulatorSchema : BaseToolSchema
    {
        [Description("Action: GetState(0) - full state report, Open(1) - open/show window, ListDevices(2) - device list, SelectDevice(3) - switch device by DeviceIndex or DeviceName, SetRotation(4) - rotate by Rotation value, SetScale(5) - set zoom by Scale value")]
        public DeviceSimulatorAction Action { get; set; } = DeviceSimulatorAction.GetState;

        [Description("Zero-based device index for SelectDevice. Takes priority over DeviceName when >= 0.")]
        public int DeviceIndex { get; set; } = -1;

        [Description("Device friendly-name substring for SelectDevice (case-insensitive). Used when DeviceIndex is -1.")]
        public string DeviceName { get; set; } = "";

        [Description("Rotation in degrees for SetRotation. Must be 0, 90, 180, or 270.")]
        public int Rotation { get; set; } = 0;

        [Description("Scale percentage for SetScale (10-100, where 100 = fit to window).")]
        public int Scale { get; set; } = 100;

        [Description("When true and Action=Open, auto-open the Simulator window if not already open. When false and Action=GetState/SelectDevice/SetRotation/SetScale, return error if window is not open rather than auto-opening.")]
        public bool AutoOpen { get; set; } = true;
    }
}
