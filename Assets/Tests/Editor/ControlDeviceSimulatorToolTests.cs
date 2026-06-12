using System.Threading.Tasks;
using NUnit.Framework;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uLoopMCP
{
    public class ControlDeviceSimulatorToolTests
    {
        private ControlDeviceSimulatorTool _tool;

        [SetUp]
        public void SetUp()
        {
            _tool = new ControlDeviceSimulatorTool();
        }

        [Test]
        public void ToolName_ReturnsCorrectName()
        {
            Assert.That(_tool.ToolName, Is.EqualTo("control-device-simulator"));
        }

        [Test]
        public async Task SetRotation_InvalidDegrees45_ReturnsFailure()
        {
            JObject paramsJson = new JObject
            {
                ["Action"] = "SetRotation",
                ["Rotation"] = 45
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            ControlDeviceSimulatorResponse response = baseResponse as ControlDeviceSimulatorResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.False);
            // Either module unavailable (acceptable) or rotation validation error
            if (response.Message.Contains("not available"))
            {
                Assert.Ignore("Device Simulator module not available in this environment; skipping rotation validation test.");
            }
            Assert.That(response.Message, Does.Contain("Rotation").Or.Contain("rotation").Or.Contain("270"));
        }

        [Test]
        public async Task SetRotation_InvalidDegrees45_WhenModuleAvailable_ReturnsValidationError()
        {
            if (!DeviceSimulatorBridge.IsModuleAvailable())
            {
                Assert.Ignore("Device Simulator module not available; skipping.");
                return;
            }

            JObject paramsJson = new JObject
            {
                ["Action"] = "SetRotation",
                ["Rotation"] = 45
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            ControlDeviceSimulatorResponse response = baseResponse as ControlDeviceSimulatorResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Does.Contain("Rotation must be 0, 90, 180, or 270, got: 45"));
        }

        [Test]
        public async Task SetScale_TooLow_WhenModuleAvailable_ReturnsValidationError()
        {
            if (!DeviceSimulatorBridge.IsModuleAvailable())
            {
                Assert.Ignore("Device Simulator module not available; skipping.");
                return;
            }

            JObject paramsJson = new JObject
            {
                ["Action"] = "SetScale",
                ["Scale"] = 5
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            ControlDeviceSimulatorResponse response = baseResponse as ControlDeviceSimulatorResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Does.Contain("Scale must be 10-100, got: 5"));
        }

        [Test]
        public async Task SetScale_TooHigh_WhenModuleAvailable_ReturnsValidationError()
        {
            if (!DeviceSimulatorBridge.IsModuleAvailable())
            {
                Assert.Ignore("Device Simulator module not available; skipping.");
                return;
            }

            JObject paramsJson = new JObject
            {
                ["Action"] = "SetScale",
                ["Scale"] = 101
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            ControlDeviceSimulatorResponse response = baseResponse as ControlDeviceSimulatorResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Does.Contain("Scale must be 10-100, got: 101"));
        }

        [Test]
        public async Task SelectDevice_NoIndexNoName_WhenModuleAvailable_ReturnsValidationError()
        {
            if (!DeviceSimulatorBridge.IsModuleAvailable())
            {
                Assert.Ignore("Device Simulator module not available; skipping.");
                return;
            }

            JObject paramsJson = new JObject
            {
                ["Action"] = "SelectDevice",
                ["DeviceIndex"] = -1,
                ["DeviceName"] = ""
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            ControlDeviceSimulatorResponse response = baseResponse as ControlDeviceSimulatorResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Does.Contain("Provide DeviceIndex >= 0 or a non-empty DeviceName"));
        }

        [Test]
        public async Task WhenModuleNotAvailable_AnyAction_ReturnsModuleUnavailableError()
        {
            // This test is only meaningful when the module is actually unavailable.
            // On Unity 6 with Device Simulator installed, it will be available,
            // so we verify the IsModuleAvailable() result is consistent with the response.
            bool moduleAvailable = DeviceSimulatorBridge.IsModuleAvailable();

            JObject paramsJson = new JObject
            {
                ["Action"] = "GetState"
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            ControlDeviceSimulatorResponse response = baseResponse as ControlDeviceSimulatorResponse;

            Assert.That(response, Is.Not.Null);

            if (!moduleAvailable)
            {
                Assert.That(response.Success, Is.False);
                Assert.That(response.Message, Does.Contain("not available"));
            }
            else
            {
                // Module available: GetState should succeed (window opens via AutoOpen=true)
                Assert.That(response.Success, Is.True);
            }
        }

        [Test]
        public async Task GetState_AutoOpenFalse_WhenWindowNotOpen_ReturnsWindowNotOpenError()
        {
            if (!DeviceSimulatorBridge.IsModuleAvailable())
            {
                Assert.Ignore("Device Simulator module not available; skipping.");
                return;
            }

            // Close the simulator window if it's open, then try GetState with AutoOpen=false
            // We can't guarantee the window is closed, so this test only checks the error message format
            // when the window happens to not be open.
            // Instead test via direct parameter path: AutoOpen=false means error if not open.
            // We just verify that the flag is honored by checking message content when failing.
            JObject paramsJson = new JObject
            {
                ["Action"] = "GetState",
                ["AutoOpen"] = false
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            ControlDeviceSimulatorResponse response = baseResponse as ControlDeviceSimulatorResponse;

            Assert.That(response, Is.Not.Null);
            if (!response.Success)
            {
                Assert.That(response.Message, Does.Contain("not open").Or.Contain("AutoOpen"));
            }
            // If window was already open, Success may be true — both are correct outcomes.
        }

        [Test]
        public async Task GetState_WithModuleAvailable_HasCorrectActionField()
        {
            if (!DeviceSimulatorBridge.IsModuleAvailable())
            {
                Assert.Ignore("Device Simulator module not available; skipping.");
                return;
            }

            JObject paramsJson = new JObject
            {
                ["Action"] = "GetState"
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            ControlDeviceSimulatorResponse response = baseResponse as ControlDeviceSimulatorResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Action, Is.EqualTo("GetState"));
        }

        [Test]
        public async Task ListDevices_WithModuleAvailable_ReturnsNonEmptyDeviceList()
        {
            if (!DeviceSimulatorBridge.IsModuleAvailable())
            {
                Assert.Ignore("Device Simulator module not available; skipping.");
                return;
            }

            JObject paramsJson = new JObject
            {
                ["Action"] = "ListDevices"
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            ControlDeviceSimulatorResponse response = baseResponse as ControlDeviceSimulatorResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.True);
            Assert.That(response.AllDeviceNames, Is.Not.Null);
            Assert.That(response.AllDeviceNames.Length, Is.GreaterThan(0));
        }
    }
}
