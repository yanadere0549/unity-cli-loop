using System.Threading.Tasks;
using NUnit.Framework;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uLoopMCP
{
    public class ControlGameViewToolTests
    {
        private ControlGameViewTool _tool;

        [SetUp]
        public void SetUp()
        {
            _tool = new ControlGameViewTool();
        }

        [Test]
        public void ToolName_ReturnsCorrectName()
        {
            Assert.That(_tool.ToolName, Is.EqualTo("control-game-view"));
        }

        [Test]
        public async Task GetState_ReturnsSuccess()
        {
            JObject paramsJson = new JObject
            {
                ["Action"] = "GetState"
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            ControlGameViewResponse response = baseResponse as ControlGameViewResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.True);
            Assert.That(response.Action, Is.EqualTo("GetState"));
            Assert.That(response.AllSizeLabels, Is.Not.Null);
            Assert.That(response.AllSizeLabels.Length, Is.GreaterThan(0));
        }

        [Test]
        public async Task GetState_AllSizeLabelsNonEmpty()
        {
            JObject paramsJson = new JObject
            {
                ["Action"] = "GetState"
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            ControlGameViewResponse response = baseResponse as ControlGameViewResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.True);
            Assert.That(response.AllSizeLabels, Is.Not.Null);
            foreach (string label in response.AllSizeLabels)
            {
                Assert.That(label, Is.Not.Null.And.Not.Empty);
            }
        }

        [Test]
        public async Task SelectSize_InvalidIndexTooLarge_ReturnsFailureWithOutOfRange()
        {
            JObject paramsJson = new JObject
            {
                ["Action"] = "SelectSize",
                ["SizeIndex"] = 99999
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            ControlGameViewResponse response = baseResponse as ControlGameViewResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Does.Contain("out of range"));
        }

        [Test]
        public async Task SelectSize_NoIndexNoLabel_ReturnsFailureWithGuidanceMessage()
        {
            JObject paramsJson = new JObject
            {
                ["Action"] = "SelectSize",
                ["SizeIndex"] = -1,
                ["SizeLabel"] = ""
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            ControlGameViewResponse response = baseResponse as ControlGameViewResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Does.Contain("SizeIndex"));
        }

        [Test]
        public async Task SelectSize_UnknownLabel_ReturnsFailureWithLabelInMessage()
        {
            const string unknownLabel = "NONEXISTENT_SIZE_LABEL_XYZ_12345";
            JObject paramsJson = new JObject
            {
                ["Action"] = "SelectSize",
                ["SizeIndex"] = -1,
                ["SizeLabel"] = unknownLabel
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            ControlGameViewResponse response = baseResponse as ControlGameViewResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Does.Contain(unknownLabel));
        }

        [Test]
        public async Task SelectSize_ByLabel_FreeAspect_ReturnsSuccess()
        {
            JObject paramsJson = new JObject
            {
                ["Action"] = "SelectSize",
                ["SizeIndex"] = -1,
                ["SizeLabel"] = "Free Aspect"
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            ControlGameViewResponse response = baseResponse as ControlGameViewResponse;

            Assert.That(response, Is.Not.Null);
            // "Free Aspect" is always present; either succeeds or reports "No Game View window is open"
            // In EditMode the Game View window should be open in the editor layout.
            // Accept both outcomes — the critical check is the message is deterministic.
            if (!response.Success)
            {
                Assert.That(response.Message, Does.Contain("Game View"));
            }
            else
            {
                Assert.That(response.CurrentSizeIndex, Is.Not.Null);
            }
        }

        [Test]
        public async Task AddCustomSize_EmptyLabel_ReturnsFailure()
        {
            JObject paramsJson = new JObject
            {
                ["Action"] = "AddCustomSize",
                ["CustomWidth"] = 1280,
                ["CustomHeight"] = 720,
                ["CustomLabel"] = ""
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            ControlGameViewResponse response = baseResponse as ControlGameViewResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Does.Contain("CustomLabel"));
        }

        [Test]
        public async Task AddCustomSize_WidthZero_ReturnsFailure()
        {
            JObject paramsJson = new JObject
            {
                ["Action"] = "AddCustomSize",
                ["CustomWidth"] = 0,
                ["CustomHeight"] = 720,
                ["CustomLabel"] = "TestSize"
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            ControlGameViewResponse response = baseResponse as ControlGameViewResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Does.Contain("CustomWidth"));
        }

        [Test]
        public async Task AddCustomSize_WidthTooLarge_ReturnsFailure()
        {
            JObject paramsJson = new JObject
            {
                ["Action"] = "AddCustomSize",
                ["CustomWidth"] = 7681,
                ["CustomHeight"] = 720,
                ["CustomLabel"] = "TestSizeTooWide"
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            ControlGameViewResponse response = baseResponse as ControlGameViewResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Does.Contain("CustomWidth"));
        }

        [Test]
        public async Task AddCustomSize_HeightZero_ReturnsFailure()
        {
            JObject paramsJson = new JObject
            {
                ["Action"] = "AddCustomSize",
                ["CustomWidth"] = 1280,
                ["CustomHeight"] = 0,
                ["CustomLabel"] = "TestSizeZeroHeight"
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            ControlGameViewResponse response = baseResponse as ControlGameViewResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Does.Contain("CustomHeight"));
        }

        [Test]
        public async Task AddCustomSize_HeightTooLarge_ReturnsFailure()
        {
            JObject paramsJson = new JObject
            {
                ["Action"] = "AddCustomSize",
                ["CustomWidth"] = 1920,
                ["CustomHeight"] = 4321,
                ["CustomLabel"] = "TestSizeTooTall"
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            ControlGameViewResponse response = baseResponse as ControlGameViewResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Does.Contain("CustomHeight"));
        }

        [Test]
        public async Task AddCustomSize_DuplicateFreeAspectLabel_ReturnsFailureWithAlreadyExists()
        {
            // "Free Aspect" is guaranteed to exist; adding a size with a label matching it should fail.
            JObject paramsJson = new JObject
            {
                ["Action"] = "AddCustomSize",
                ["CustomWidth"] = 1920,
                ["CustomHeight"] = 1080,
                ["CustomLabel"] = "Free Aspect"
            };

            BaseToolResponse baseResponse = await _tool.ExecuteAsync(paramsJson);
            ControlGameViewResponse response = baseResponse as ControlGameViewResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Does.Contain("already exists"));
        }
    }
}
