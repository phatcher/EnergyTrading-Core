﻿namespace EnergyTrading.UnitTest.FileProcessing.LoopBased
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using EnergyTrading.FileProcessing;
    using EnergyTrading.FileProcessing.FileHandling;
    using EnergyTrading.FileProcessing.FileProcessors;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class PollingBasedFileProcessorTests
    {
        private const string DropDirectoryName = "drop";
        private const string AcknowledgedDirectoryName = "acknowledged";
        private const string TempDirectoryName = "temp";

        private FileProcessorEndpoint endpoint; 
        private string dropLocation;
        private string acknowledgedLocation;
        private string tempLocation;

        [SetUp]
        public void CreateDirectories()
        {
            DeleteDirectories();

            dropLocation = Directory.CreateDirectory(DropDirectoryName).FullName;
            acknowledgedLocation = Directory.CreateDirectory(AcknowledgedDirectoryName).FullName;
            tempLocation = Directory.CreateDirectory(TempDirectoryName).FullName;

            endpoint = new FileProcessorEndpoint
            {
                DropPath = dropLocation,
                InProgressPath = acknowledgedLocation,
            };
        }

        [TearDown]
        public void DeleteDirectories()
        {
            RemoveDirectory(DropDirectoryName);
            RemoveDirectory(AcknowledgedDirectoryName);
            RemoveDirectory(TempDirectoryName);
        }

        [Test]
        public void StartExistingFileInDropLocationProcessesFile()
        {
            var fileData = CreateTestFileIn(dropLocation);
            TestFilePickup(fileData.Filename, fileData.Content);
        }

        private void TestFilePickup(string originalFilename, string originalContent)
        {
            var resetEvent = new AutoResetEvent(false);
            var mockHandler = new Mock<IHandleFiles>();

            using (var listener = new PollingBasedFileProcessor(endpoint, mockHandler.Object, new DefaultFileFilter()))
            {
                listener.Start();
                resetEvent.WaitOne(1000);
            }

            mockHandler.Verify(x => x.Notify(It.IsAny<ProcessingFile>()));
            AssertDirectoryIsEmpty(dropLocation);

            var filePaths = Directory.GetFiles(acknowledgedLocation);
            Assert.AreEqual(filePaths.Length, 1);

            var actualFile = new FileInfo(filePaths[0]);
            AssertFileMatches(originalContent, originalFilename, actualFile);
        }

        [Test]
        public void StartDropNewFileProcessFile()
        {
            var fileData = CreateTestFileIn(tempLocation);

            var resetEvent = new AutoResetEvent(false);
            var mockHandler = new Mock<IHandleFiles>();

            using (var listener = new PollingBasedFileProcessor(endpoint, mockHandler.Object, new DefaultFileFilter()))
            {
                listener.Start();

                new FileInfo(Path.Combine(tempLocation, fileData.Filename)).MoveTo(Path.Combine(dropLocation, fileData.Filename));

                resetEvent.WaitOne(1000);
            }

            mockHandler.Verify(x => x.Notify(It.IsAny<ProcessingFile>()));
            AssertDirectoryIsEmpty(dropLocation);

            var filePaths = Directory.GetFiles(acknowledgedLocation);
            Assert.AreEqual(filePaths.Length, 1);

            var actualFile = new FileInfo(filePaths[0]);
            AssertFileMatches(fileData.Content, fileData.Filename, actualFile);
        }

        [Test]
        public void FilterPreventsProcessing()
        {
            var fileData = CreateTestFileIn(tempLocation);

            var resetEvent = new AutoResetEvent(false);
            var mockHandler = new Mock<IHandleFiles>();

            using (var listener = new PollingBasedFileProcessor(endpoint, mockHandler.Object, new FalseFileFilter()))
            {
                listener.Start();

                new FileInfo(Path.Combine(tempLocation, fileData.Filename)).MoveTo(Path.Combine(dropLocation, fileData.Filename));

                resetEvent.WaitOne(1000);
            }

            mockHandler.Verify(x => x.Notify(It.IsAny<ProcessingFile>()), Times.Never);

            var filePaths = Directory.GetFiles(dropLocation);
            Assert.AreEqual(filePaths.Length, 1);

            var actualFile = new FileInfo(filePaths[0]);
            AssertFileMatches(fileData.Content, fileData.Filename, actualFile);
        }

        private static void RemoveDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return;
            }
            foreach (var filePath in Directory.GetFiles(directory))
            {
                new FileInfo(filePath).Delete();
            }
            Directory.Delete(directory, true);
        }

        private FileMetaData CreateTestFileIn(string directory)
        {
            const string Filename = "test.file";
            var content = string.Format("{0} {1}", DateTime.UtcNow.ToLongDateString(), DateTime.UtcNow.ToLongTimeString());
            var file = new FileInfo(Path.Combine(directory, Filename));
            using (var s = file.CreateText())
            {
                s.Write(content);
            }

            return new FileMetaData { Filename = Filename, Content = content };
        }

        private void AssertDirectoryIsEmpty(string directory)
        {
            Assert.IsFalse(Directory.EnumerateFiles(directory).Any());
        }

        private static void AssertFileMatches(string originalContent, string originalFilename, FileInfo actualFile)
        {
            Assert.IsTrue(actualFile.Name.Contains(originalFilename));
            using (var sr = actualFile.OpenText())
            {
                Assert.AreEqual(sr.ReadToEnd(), originalContent);
            }
        }

        public class FalseFileFilter : IFileFilter
        {
            public bool IncludeFile(string fullFilePath)
            {
                return false;
            }
        }

        public class FileMetaData
        {
            public string Filename { get; set; }
            public string Content { get; set; }
        }
    }
}
