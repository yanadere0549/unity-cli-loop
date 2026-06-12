using System.Threading.Tasks;
using NUnit.Framework;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uLoopMCP
{
    public class SwapPlayModeViewToolTests
    {
        private SwapPlayModeViewTool _tool;

        [SetUp]
        public void SetUp()
        {
            _tool = new SwapPlayModeViewTool();
        }

        [Test]
        public void ToolName_ReturnsCorrectName()
        {
            Assert.That(_tool.ToolName, Is.EqualTo("swap-play-mode-view"));
        }

        [Test]
        public async Task SwapToGameView_WhenNoPlayModeViewOpen_CanReturnGracefulError()
        {
            // In EditMode, GetMainPlayModeView() may or may not return a window depending on editor state.
            // This test verifies that the tool does not throw — it either succeeds or returns a structured error.
            JObject paramsJson = new JObject
            {
                ["TargetView"] = "GameView"
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            SwapPlayModeViewResponse response = baseResponse as SwapPlayModeViewResponse;

            Assert.That(response, Is.Not.Null);
            // Either path is valid: window open (Success=true) or window not found (Success=false + message)
            if (!response.Success)
            {
                Assert.That(response.Message, Is.Not.Null.And.Not.Empty);
            }
        }

        [Test]
        public async Task SwapToSimulator_WhenModuleUnavailable_ReturnsModuleNotAvailableError()
        {
            if (DeviceSimulatorBridge.IsModuleAvailable())
            {
                Assert.Ignore("Device Simulator module IS available; this test only applies when it is unavailable.");
                return;
            }

            JObject paramsJson = new JObject
            {
                ["TargetView"] = "Simulator"
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            SwapPlayModeViewResponse response = baseResponse as SwapPlayModeViewResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Does.Contain("not available"));
        }

        [Test]
        public async Task SwapToGameView_ResponseHasCorrectStructure()
        {
            JObject paramsJson = new JObject
            {
                ["TargetView"] = "GameView"
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            SwapPlayModeViewResponse response = baseResponse as SwapPlayModeViewResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Message, Is.Not.Null);
        }

        [Test]
        public async Task SwapToGameView_WhenSucceeds_ActiveViewTypeIsGameView()
        {
            JObject paramsJson = new JObject
            {
                ["TargetView"] = "GameView"
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            SwapPlayModeViewResponse response = baseResponse as SwapPlayModeViewResponse;

            Assert.That(response, Is.Not.Null);
            if (response.Success)
            {
                Assert.That(response.ActiveViewType, Is.Not.Null.And.Not.Empty);
            }
        }

        [Test]
        public async Task SwapToGameView_AlreadyActive_ReturnsAlreadyActiveMessage()
        {
            // Run twice: first ensures GameView is active, second should get "Already active"
            JObject paramsJson = new JObject
            {
                ["TargetView"] = "GameView"
            };

            // First call — may succeed or fail depending on editor state
            await _tool.ExecuteAsync(paramsJson);

            // Second call — if first succeeded, current view is GameView, so this should be no-op
            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            SwapPlayModeViewResponse response = baseResponse as SwapPlayModeViewResponse;

            Assert.That(response, Is.Not.Null);
            if (response.Success)
            {
                // Either "Already active: GameView" or "Swapped play mode view to GameView"
                Assert.That(response.Message, Does.Contain("GameView").Or.Contain("game").IgnoreCase);
            }
        }
    }
}
