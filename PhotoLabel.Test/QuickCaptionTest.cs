using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;

namespace PhotoLabel.Test
{
    [TestClass]
    public class QuickCaptionTest
    {
        private const string TestCaption = "This is a test caption";

        [TestMethod]
        public void AddQuickCaption()
        {
            var logService = new Mock<Services.ILogService>().Object;
            var quickCaptionService = new Services.QuickCaptionService(logService);
            
            // create the observer
            var observer = new Observer();

            // observe changes to the quick caption
            quickCaptionService.Subscribe(observer);

            // create the test metadata
            var testMetadata = new Services.Models.Metadata
            {
                Caption = TestCaption,
                DateTaken = "10/10/1910"
            };

            // add a caption
            quickCaptionService.Add("filename", testMetadata);

            // now retrieve the captions
            quickCaptionService.Switch("filename", testMetadata);

            Assert.AreEqual(2, observer.Captions.Count);
        }

        private class Observer : Services.IQuickCaptionObserver
        {
            public List<string> Captions { get; set; } = new List<string>();

            public void OnClear()
            {
                Captions.Clear();
            }

            public void OnCompleted()
            {
                // no action required
            }

            public void OnError(Exception error)
            {
                throw error;
            }

            public void OnNext(string caption)
            {
                Captions.Add(caption);
            }
        }
    }
}
