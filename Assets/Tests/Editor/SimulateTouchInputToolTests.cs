using System.Threading.Tasks;
using NUnit.Framework;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uLoopMCP
{
    /// <summary>
    /// EditMode unit tests for SimulateTouchInputTool.
    ///
    /// Note: ULOOPMCP_HAS_INPUT_SYSTEM is defined only in the uLoopMCP.Editor assembly, not here.
    /// From the test assembly's perspective the tool always takes the
    /// "Input System not available" or "PlayMode not active" early-exit path,
    /// depending on whether the compiled tool binary was built with or without Input System.
    /// Either failure message is correct for EditMode.
    /// </summary>
    public class SimulateTouchInputToolTests
    {
        private SimulateTouchInputTool _tool;

        [SetUp]
        public void SetUp()
        {
            _tool = new SimulateTouchInputTool();
        }

        [Test]
        public void ToolName_ReturnsCorrectName()
        {
            Assert.That(_tool.ToolName, Is.EqualTo("simulate-touch-input"));
        }

        [Test]
        public async Task ExecuteAsync_InEditMode_ReturnsFailure()
        {
            // In EditMode, the tool must fail — either because:
            //   (a) ULOOPMCP_HAS_INPUT_SYSTEM is not defined → "Input System" message, or
            //   (b) Input System is available but PlayMode is not active → "PlayMode" message.
            JObject paramsJson = new JObject
            {
                ["Action"] = "Tap",
                ["X"] = 100f,
                ["Y"] = 100f
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            SimulateTouchInputResponse response = baseResponse as SimulateTouchInputResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.False);
            bool isPlayModeError = response.Message.Contains("PlayMode");
            bool isInputSystemError = response.Message.Contains("Input System");
            Assert.That(isPlayModeError || isInputSystemError, Is.True,
                $"Expected PlayMode or Input System error, got: {response.Message}");
        }

        [Test]
        public async Task ExecuteAsync_TouchIdOutOfRange_ReturnsFailure()
        {
            // In EditMode, the PlayMode or Input System guard fires before parameter validation.
            // Verifies the tool returns a structured failure (not an exception).
            JObject paramsJson = new JObject
            {
                ["Action"] = "Tap",
                ["X"] = 100f,
                ["Y"] = 100f,
                ["TouchId"] = 10
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            SimulateTouchInputResponse response = baseResponse as SimulateTouchInputResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task ExecuteAsync_LongPressDurationZero_ReturnsFailure()
        {
            // In EditMode, the PlayMode or Input System guard fires before Duration validation.
            JObject paramsJson = new JObject
            {
                ["Action"] = "LongPress",
                ["X"] = 100f,
                ["Y"] = 100f,
                ["Duration"] = 0f
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            SimulateTouchInputResponse response = baseResponse as SimulateTouchInputResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task ExecuteAsync_DragSpeedNegative_ReturnsFailure()
        {
            // In EditMode, the PlayMode or Input System guard fires before DragSpeed validation.
            JObject paramsJson = new JObject
            {
                ["Action"] = "Drag",
                ["X"] = 500f,
                ["Y"] = 300f,
                ["FromX"] = 100f,
                ["FromY"] = 300f,
                ["DragSpeed"] = -1f
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            SimulateTouchInputResponse response = baseResponse as SimulateTouchInputResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task ExecuteAsync_AnyCallInEditMode_ReturnsNonNullMessage()
        {
            // Smoke test: no call should throw; response and message must always be non-null.
            JObject paramsJson = new JObject
            {
                ["Action"] = "Tap",
                ["X"] = 960f,
                ["Y"] = 540f,
                ["TouchId"] = 0
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            SimulateTouchInputResponse response = baseResponse as SimulateTouchInputResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Message, Is.Not.Null);
        }

        [Test]
        public async Task ExecuteAsync_ActionFieldIsSet_InFailureResponse()
        {
            // Even in a failure response the Action field should reflect the requested action.
            JObject paramsJson = new JObject
            {
                ["Action"] = "Tap",
                ["X"] = 100f,
                ["Y"] = 100f
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            SimulateTouchInputResponse response = baseResponse as SimulateTouchInputResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Action, Is.Not.Null.And.Not.Empty);
        }
    }
}
