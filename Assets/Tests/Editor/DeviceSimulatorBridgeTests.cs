using NUnit.Framework;

namespace io.github.hatayama.uLoopMCP
{
    /// <summary>
    /// Unit tests for DeviceSimulatorBridge reflection layer.
    /// Tests involving the window (GetMain, GetDeviceNames, etc.) require the Simulator window to be open;
    /// those are skipped if the module is not available.
    /// </summary>
    public class DeviceSimulatorBridgeTests
    {
        [Test]
        public void IsModuleAvailable_ReturnsTrue_OnUnity6()
        {
#if UNITY_6000_0_OR_NEWER
            // On Unity 6000.0 the Device Simulator module should always be available
            bool available = DeviceSimulatorBridge.IsModuleAvailable();
            Assert.That(available, Is.True,
                "DeviceSimulatorBridge.IsModuleAvailable() should return true on Unity 6000.0");
#else
            Assert.Ignore("This test only asserts availability on Unity 6000.0 or newer.");
#endif
        }

        [Test]
        public void FindSimulatorWindow_DoesNotThrow()
        {
            // Whether the window is open or not, FindSimulatorWindow must never throw
            Assert.DoesNotThrow(() =>
            {
                _ = DeviceSimulatorBridge.FindSimulatorWindow();
            });
        }

        [Test]
        public void OpenWindow_WhenModuleAvailable_DoesNotThrow()
        {
            if (!DeviceSimulatorBridge.IsModuleAvailable())
            {
                Assert.Ignore("Device Simulator module not available; skipping.");
                return;
            }

            Assert.DoesNotThrow(() => DeviceSimulatorBridge.OpenWindow());
        }

        [Test]
        public void GetDeviceNames_WithNullMain_ReturnsEmptyArray()
        {
            // Passing null should not throw and should return an empty array
            string[] names = DeviceSimulatorBridge.GetDeviceNames(null);
            Assert.That(names, Is.Not.Null);
            Assert.That(names.Length, Is.EqualTo(0));
        }

        [Test]
        public void GetDeviceIndex_WithNullMain_ReturnsNegativeOne()
        {
            int index = DeviceSimulatorBridge.GetDeviceIndex(null);
            Assert.That(index, Is.EqualTo(-1));
        }

        [Test]
        public void GetScale_WithNullMain_ReturnsNegativeOne()
        {
            int scale = DeviceSimulatorBridge.GetScale(null);
            Assert.That(scale, Is.EqualTo(-1));
        }

        [Test]
        public void GetRotationDegrees_WithNullMain_ReturnsNegativeOne()
        {
            int rotation = DeviceSimulatorBridge.GetRotationDegrees(null);
            Assert.That(rotation, Is.EqualTo(-1));
        }

        [Test]
        public void GetScreenWidth_WithNullMain_ReturnsNegativeOne()
        {
            int width = DeviceSimulatorBridge.GetScreenWidth(null);
            Assert.That(width, Is.EqualTo(-1));
        }

        [Test]
        public void GetScreenHeight_WithNullMain_ReturnsNegativeOne()
        {
            int height = DeviceSimulatorBridge.GetScreenHeight(null);
            Assert.That(height, Is.EqualTo(-1));
        }

        [Test]
        public void GetOrientationName_WithNullMain_ReturnsEmptyString()
        {
            string orientation = DeviceSimulatorBridge.GetOrientationName(null);
            Assert.That(orientation, Is.Not.Null);
            Assert.That(orientation, Is.EqualTo(string.Empty));
        }

        [Test]
        public void GetMain_WithNullWindow_ReturnsNull()
        {
            object main = DeviceSimulatorBridge.GetMain(null);
            Assert.That(main, Is.Null);
        }
    }
}
