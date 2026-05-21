using System;
using NUnit.Framework;
using TimesheetApp.Interfaces;

namespace TimesheetApp.UnitTests
{
    [TestFixture]
    public class TimeLoggerTests
    {
        [Test]
        public void LogTime_ValidData_TimeIsSuccessfullyLogged()
        {
            var fakeUserLogger = new FakeUserLogger();
            fakeUserLogger.UserName = "john.doe";
            fakeUserLogger.UserEmail = "john.doe@example.com";

            var fakeTaskManager = new FakeTaskManager();
            fakeTaskManager.TaskId = 42;

            var fakeTask = new FakeTask();
            fakeTask.SaveResult = true;

            var fakeEmailSender = new FakeEmailSender();
            var fakeErrorLogger = new FakeErrorLogger();

            var timeLogger = new TimeLogger(
                fakeUserLogger,
                fakeTaskManager,
                fakeTask,
                fakeEmailSender,
                fakeErrorLogger
            );

            Assert.DoesNotThrow(new TestDelegate(() => timeLogger.LogTime(2, 30, "Implementacija funkcionalnosti")));
        }

        [Test]
        public void LogTime_SaveToDBFails_ErrorIsLoggedWithCorrectMessage()
        {
            var fakeUserLogger = new FakeUserLogger();
            fakeUserLogger.UserName = "john.doe";
            fakeUserLogger.UserEmail = "john.doe@example.com";

            var fakeTaskManager = new FakeTaskManager();
            fakeTaskManager.TaskId = 42;

            var fakeTask = new FakeTask();
            fakeTask.SaveResult = false;

            var fakeEmailSender = new FakeEmailSender();
            var mockErrorLogger = new MockErrorLogger();

            var timeLogger = new TimeLogger(
                fakeUserLogger,
                fakeTaskManager,
                fakeTask,
                fakeEmailSender,
                mockErrorLogger
            );

            Assert.Throws<Exception>(new TestDelegate(() => timeLogger.LogTime(1, 0, "Test opis")));

            Assert.IsNotNull(mockErrorLogger.LoggedError);
            Assert.That(mockErrorLogger.LoggedError.Message, Does.Contain("Failed to save data to database"));
        }

        [Test]
        public void LogTime_GetLoggedUserEmailThrowsException_ErrorIsLoggedWithMessage_FailedToGetUserEmail()
        {
            // Fixed: Using hand-written Fake instead of NSubstitute to avoid modern extensions rule
            var fakeUserLogger = new FakeUserLogger();
            fakeUserLogger.UserName = "john.doe";
            fakeUserLogger.ExceptionToThrowOnEmail = new Exception("Failed to get user email");

            var fakeTaskManager = new FakeTaskManager();
            var fakeTask = new FakeTask();
            var fakeEmailSender = new FakeEmailSender();
            var mockErrorLogger = new MockErrorLogger();

            var timeLogger = new TimeLogger(
                fakeUserLogger,
                fakeTaskManager,
                fakeTask,
                fakeEmailSender,
                mockErrorLogger
            );

            var ex = Assert.Throws<Exception>(new TestDelegate(() => timeLogger.LogTime(1, 0, "Test opis")));

            Assert.IsNotNull(mockErrorLogger.LoggedError);
            Assert.AreEqual("Failed to get user email", mockErrorLogger.LoggedError.Message);
            Assert.AreEqual("Failed to get user email", ex.Message);
        }

        [Test]
        public void LogTime_GetTaskIdThrowsException_ErrorIsLoggedWithMessage_FailedToGetTheTaskInfo()
        {
            var fakeUserLogger = new FakeUserLogger();
            fakeUserLogger.UserName = "john.doe";
            fakeUserLogger.UserEmail = "john.doe@example.com";

            var fakeTaskManager = new FakeTaskManager();
            fakeTaskManager.ExceptionToThrow = new Exception("Failed to get the task info");

            var fakeTask = new FakeTask();
            var fakeEmailSender = new FakeEmailSender();
            var mockErrorLogger = new MockErrorLogger();

            var timeLogger = new TimeLogger(
                fakeUserLogger,
                fakeTaskManager,
                fakeTask,
                fakeEmailSender,
                mockErrorLogger
            );

            var ex = Assert.Throws<Exception>(new TestDelegate(() => timeLogger.LogTime(1, 0, "Test opis")));

            Assert.IsNotNull(mockErrorLogger.LoggedError);
            Assert.AreEqual("Failed to get the task info", mockErrorLogger.LoggedError.Message);
            Assert.AreEqual("Failed to get the task info", ex.Message);
        }

        [Test]
        public void LogTime_SendEmailThrowsException_ErrorIsLoggedWithMessage_FailedToSendEmail()
        {
            var fakeUserLogger = new FakeUserLogger();
            fakeUserLogger.UserName = "john.doe";
            fakeUserLogger.UserEmail = "john.doe@example.com";

            var fakeTaskManager = new FakeTaskManager();
            fakeTaskManager.TaskId = 42;

            var fakeTask = new FakeTask();
            fakeTask.SaveResult = true;

            // Fixed: Using hand-written Fake to simulate the Email server crash safely in 7.3
            var fakeEmailSender = new FakeEmailSender();
            fakeEmailSender.ExceptionToThrow = new Exception("Failed to send email");

            var mockErrorLogger = new MockErrorLogger();

            var timeLogger = new TimeLogger(
                fakeUserLogger,
                fakeTaskManager,
                fakeTask,
                fakeEmailSender,
                mockErrorLogger
            );

            var ex = Assert.Throws<Exception>(new TestDelegate(() => timeLogger.LogTime(2, 30, "Test opis")));

            Assert.IsNotNull(mockErrorLogger.LoggedError);
            Assert.AreEqual("Failed to send email", mockErrorLogger.LoggedError.Message);
            Assert.AreEqual("Failed to send email", ex.Message);
        }
    }

    // =====================================================================
    // Pure C# 7.3 Fakes and Mocks (No Framework Magic Needed)
    // =====================================================================

    internal class FakeUserLogger : IUserLogger
    {
        public string UserName { get; set; } = "test.user";
        public string UserEmail { get; set; } = "test.user@example.com";
        public Exception ExceptionToThrowOnName { get; set; } = null;
        public Exception ExceptionToThrowOnEmail { get; set; } = null;

        public string GetLoggedUserName()
        {
            if (ExceptionToThrowOnName != null)
                throw ExceptionToThrowOnName;
            return UserName;
        }

        public string GetLoggedUserEmail(string userName)
        {
            if (ExceptionToThrowOnEmail != null)
                throw ExceptionToThrowOnEmail;
            return UserEmail;
        }
    }

    internal class FakeTaskManager : ITaskManager
    {
        public int TaskId { get; set; } = 1;
        public Exception ExceptionToThrow { get; set; } = null;

        public int GetTaskId(string userName, string userEmail)
        {
            if (ExceptionToThrow != null)
                throw ExceptionToThrow;
            return TaskId;
        }
    }

    internal class FakeTask : ITask
    {
        public bool SaveResult { get; set; } = true;

        public int TaskId { get; set; }
        public int Hours { get; set; }
        public int Minutes { get; set; }
        public string Description { get; set; }

        public bool SaveToDB()
        {
            return SaveResult;
        }
    }

    internal class FakeEmailSender : IEmailSender
    {
        public Exception ExceptionToThrow { get; set; } = null;

        public void SendEmail(string to, string subject, string body)
        {
            if (ExceptionToThrow != null)
                throw ExceptionToThrow;
        }
    }

    internal class FakeErrorLogger : IErrorLogger
    {
        public void LogError(Exception ex)
        {
        }
    }

    internal class MockErrorLogger : IErrorLogger
    {
        public Exception LoggedError { get; private set; } = null;

        public void LogError(Exception ex)
        {
            LoggedError = ex;
        }
    }
}