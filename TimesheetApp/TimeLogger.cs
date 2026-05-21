using System;
using TimesheetApp.Interfaces;
using TimesheetApp.Util;
using TimesheetApp.DBAccess;

namespace TimesheetApp
{
    public class TimeLogger
    {
        ITask task;
        IEmailSender emailSender;
        IErrorLogger errorLogger;
        IUserLogger userLogger;
        ITaskManager taskManager;

        // Originalni konstruktor - koristi konkretne implementacije (produkcija)
        public TimeLogger()
        {
            task = new TaskLogger();
            emailSender = new EmailSender();
            errorLogger = new ErrorLogger();
            userLogger = new UserLogger();
            taskManager = new TaskManager();
        }

        // Novi konstruktor za ubacivanje lažnih implementacija (testiranje)
        // Dependency injection na nivou konstruktora
        public TimeLogger(
            IUserLogger userLogger,
            ITaskManager taskManager,
            ITask task,
            IEmailSender emailSender,
            IErrorLogger errorLogger)
        {
            this.userLogger = userLogger;
            this.taskManager = taskManager;
            this.task = task;
            this.emailSender = emailSender;
            this.errorLogger = errorLogger;
        }

        public void LogTime(int hours, int minutes, string description)
        {
            try
            {
                // Korak 1: Dobaviti podatke o trenutno logovanom korisniku (username, email)
                string userName = userLogger.GetLoggedUserName();
                string userEmail = userLogger.GetLoggedUserEmail(userName);

                // Korak 2: Dobaviti podatke o zadatku (taskId)
                int taskId = taskManager.GetTaskId(userName, userEmail);

                // Korak 3: Logovati vreme - sačuvati podatke u bazi
                task.TaskId = taskId;
                task.Hours = hours;
                task.Minutes = minutes;
                task.Description = description;
                bool saved = task.SaveToDB();

                if (saved)
                {
                    // Korak 4: Poslati mejl obaveštenja o logovanom vremenu
                    emailSender.SendEmail(
                        userEmail,
                        "Time logged successfully",
                        hours + " hours and " + minutes + " minutes successfully logged to task with ID=" + taskId
                    );
                }
                else
                {
                    throw new Exception("Failed to save data to database");
                }
            }
            catch (Exception ex) // Korak 5: Obrada grešaka
            {
                errorLogger.LogError(ex);
                throw ex;
            }
        }
    }
}