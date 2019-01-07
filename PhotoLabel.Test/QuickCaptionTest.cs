using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PhotoLabel.Services;

namespace PhotoLabel.Test
{
    [TestClass]
    public class QuickCaptionTest
    {
        private const string TestCaption = "This is a test caption";

        [TestMethod]
        public void SetQuickCaption()
        {
            var logService = new Mock<ILogService>().Object;

            // create the service
            var quickCaptionService = new QuickCaptionService(logService);

            // add an item to the service
            quickCaptionService.Add("01/01/2001", TestCaption);

            // get an item from the service
            var captions = quickCaptionService.Get("01/01/2001");
            Assert.AreEqual(TestCaption, captions.First());
        }
    }
}
