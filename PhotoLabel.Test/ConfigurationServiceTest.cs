using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Drawing;
using System.Windows.Forms;

namespace PhotoLabel.Test
{
    [TestClass]
    public class ConfigurationServiceTest
    {
        #region variables
        private static Services.ILogService _logService;
        private static Services.IXmlFileSerialiser _xmlFileSerialiser;
        private static TestContext _testContext;
        #endregion

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            var xmlFileSerialiserMock = new Mock<Services.IXmlFileSerialiser>();
            xmlFileSerialiserMock.Setup(o => o.Deserialise<Services.Models.Configuration>(It.IsAny<string>()))
                .Returns(() => new Services.Models.Configuration());
            _xmlFileSerialiser = xmlFileSerialiserMock.Object;

            _logService = new Mock<Services.ILogService>().Object;

            _testContext = testContext;
        }

        [TestMethod]
        public void BackgroundColour()
        {
            var configurationService = new Services.ConfigurationService(_logService, _xmlFileSerialiser)
            {
                BackgroundColour=Color.AliceBlue
            };

            // test the get
            var backgroundColour = configurationService.BackgroundColour;

            Assert.AreEqual(Color.AliceBlue.ToArgb(), backgroundColour.ToArgb());
        }

        [TestMethod]
        public void BackgroundSecondColour()
        {
            var configurationService = new Services.ConfigurationService(_logService, _xmlFileSerialiser)
            {
                BackgroundSecondColour = Color.AliceBlue
            };

            // get the new value
            var newValue = configurationService.BackgroundSecondColour;

            Assert.AreEqual(Color.AliceBlue.ToArgb(), newValue.Value.ToArgb());
        }


        [TestMethod]
        public void CaptionAlignment()
        {
            var configurationService = new Services.ConfigurationService(_logService, _xmlFileSerialiser)
            {
                CaptionAlignment = Services.CaptionAlignments.BottomCentre
            };

            // test the get
            var captionAlignment = configurationService.CaptionAlignment;

            Assert.AreEqual(Services.CaptionAlignments.BottomCentre, captionAlignment);
        }

        [TestMethod]
        public void Colour()
        {
            var configurationService = new Services.ConfigurationService(_logService, _xmlFileSerialiser)
            {
                Colour = Color.Purple
            };

            // test the get
            var colour = configurationService.Colour;

            Assert.AreEqual(Color.Purple.ToArgb(), colour.ToArgb());
        }

        [TestMethod]
        public void FontBold()
        {
            var configurationService = new Services.ConfigurationService(_logService, _xmlFileSerialiser);

                // get the current value
                var oldFontBold = configurationService.FontBold;

            // set the value
            configurationService.FontBold = !oldFontBold;

            // get the value again
            var newFontBold = configurationService.FontBold;

            Assert.AreEqual(!oldFontBold, newFontBold);
        }

        [TestMethod]
        public void FontName()
        {
            var configurationService = new Services.ConfigurationService(_logService, _xmlFileSerialiser);

            // get the current value
            var fontName = configurationService.FontName;
            try
            {
                // set the value
                configurationService.FontName = "Wayne";

                // get the value again
                var newFontName = configurationService.FontName;

                Assert.AreEqual("Wayne", newFontName);
            }
            finally
            {
                configurationService.FontName = fontName;
            }
        }

        [TestMethod]
        public void FontSize()
        {
            var configurationService = new Services.ConfigurationService(_logService, _xmlFileSerialiser);

            // get the current value
            var oldFontSize = configurationService.FontSize;

            // set the new value
            configurationService.FontSize = oldFontSize + 1;

            // get the new value
            var newFontSize = configurationService.FontSize;

            Assert.AreEqual(oldFontSize+1,newFontSize);
        }

        [TestMethod]
        public void FontType()
        {
            var configurationService = new Services.ConfigurationService(_logService, _xmlFileSerialiser);

            // get the current value
            var oldFontType = configurationService.FontType;

            // get the new value
            var newValue = oldFontType == "%" ? "pts" : "%";

            // set the value
            configurationService.FontType = newValue;

            // get the new value
            var newFontType = configurationService.FontType;

            Assert.AreEqual(newValue, newFontType);
        }

        [TestMethod]
        public void ImageFormat()
        {
            var configurationService = new Services.ConfigurationService(_logService, _xmlFileSerialiser);

            // get the current value
            var oldImageFormat = configurationService.ImageFormat;

            // get the new value
            var newValue = oldImageFormat == Services.ImageFormat.Jpeg
                ? Services.ImageFormat.Png
                : Services.ImageFormat.Jpeg;

            // set the value
            configurationService.ImageFormat = newValue;

            // get the new value
            var newImageFormat = configurationService.ImageFormat;

            Assert.AreEqual(newValue, newImageFormat);
        }

        [TestMethod]
        public void OutputPath()
        {
            var configurationService = new Services.ConfigurationService(_logService, _xmlFileSerialiser)
            {
                OutputPath=_testContext.TestRunDirectory
            };

            // get the new value
            var newOutputPath = configurationService.OutputPath;

            Assert.AreEqual(_testContext.TestRunDirectory, newOutputPath);
        }

        [TestMethod]
        public void SecondColour()
        {
            var configurationService = new Services.ConfigurationService(_logService, _xmlFileSerialiser)
            {
                SecondColour = Color.AliceBlue
            };

            // get the new value
            var newValue = configurationService.SecondColour;

            Assert.AreEqual(Color.AliceBlue.ToArgb(), newValue.Value.ToArgb());
        }

        [TestMethod]
        public void WindowState()
        {
            var configurationService = new Services.ConfigurationService(_logService, _xmlFileSerialiser)
            {
                WindowState = FormWindowState.Maximized
            };

            // get the new value
            var newValue = configurationService.WindowState;

            Assert.AreEqual(FormWindowState.Maximized, newValue);
        }
    }
}