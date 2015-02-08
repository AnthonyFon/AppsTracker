﻿#region Licence
/*
  *  Author: Marko Devcic, madevcic@gmail.com
  *  Copyright: Marko Devcic, 2014
  *  Licence: http://creativecommons.org/licenses/by-nc-nd/4.0/
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Timers;
using System.Web.UI;

using AppsTracker.DAL.Service;
using AppsTracker.Models.EntityModels;

namespace AppsTracker.Logging
{
    internal sealed class EmailHelper : IDisposable
    {
        #region Fields

        bool _disposed = false;
        double _interval;
        int _smtpPort;
        string _smtpHost;
        string _smtpUsername;
        string _smtpPassword;
        string _emailTo;
        string _emailFrom;
        bool _ssl;

        DateTime _dateNow;
        DateTime _dateLast;
        System.Timers.Timer _timer;

        IAppsService _appsService;
        ISettingsService _settingsService;


        #endregion

        #region Properties

        public double Interval
        {
            get
            {
                return _interval;
            }
            set
            {
                _interval = value;
                if (_timer != null) _timer.Interval = value;
            }
        }

        public int SmtpPort
        {
            get
            {
                return _smtpPort;
            }
            set
            {
                _smtpPort = value;
            }
        }

        public string SmtpHost
        {
            get
            {
                return _smtpHost;
            }
            set
            {
                _smtpHost = value;
            }
        }

        public string SmtpUsername
        {
            get
            {
                return _smtpUsername;
            }
            set
            {
                _smtpUsername = value;
            }
        }

        public string SmtpPassword
        {
            get
            {
                return _smtpPassword;
            }
            set
            {
                _smtpPassword = value;
            }
        }

        public string EmailTo
        {
            get
            {
                return _emailTo;
            }
            set
            {
                _emailTo = value;
            }
        }

        public string EmailFrom
        {
            get
            {
                return _emailFrom;
            }
            set
            {
                _emailFrom = value;
            }
        }

        public bool SSL
        {
            get
            {
                return _ssl;
            }
            set
            {
                _ssl = value;
            }

        }

        #endregion

        #region Constructor

        public EmailHelper()
        {
            _appsService = ServiceFactory.Get<IAppsService>();
            _settingsService = ServiceFactory.Get<ISettingsService>();

            _timer = new System.Timers.Timer();
            _timer.Interval = _interval = _settingsService.Settings.EmailInterval;
            _timer.AutoReset = true;
            _timer.Elapsed += timer_Elapsed;
            _timer.Start();
        }

        #endregion

        #region Event Handlers

        private async void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var logsTask = GetLogsByDateAsync();
            var usageTask = GetCurrentLoginAsync();
            await Task.WhenAll(logsTask, usageTask);

            string emailBody = GenerateEmailBody(logsTask.Result, usageTask.Result);
            string attachment = await GetZipFile(logsTask.Result);

            await SendEmailAsync(emailBody, attachment);
        }

        #endregion

        #region Class Methods

        private Task SendEmailAsync(string emailBody, string attachment)
        {
            return Task.Run(() =>
            {
                try
                {
                    using (MailMessage mailMessage = new MailMessage(new MailAddress(EmailFrom), new MailAddress(EmailTo)))
                    {
                        mailMessage.Subject = string.Format("Apps Tracker Report {0} {1} to {2}", DateTime.Now.ToShortDateString(), _dateLast.ToShortTimeString(), _dateNow.ToShortTimeString());
                        mailMessage.IsBodyHtml = true;
                        mailMessage.Body = emailBody;
                        mailMessage.Attachments.Add(new Attachment(attachment));

                        SmtpClient smtpClient = new SmtpClient();
                        smtpClient.UseDefaultCredentials = false;
                        smtpClient.Host = SmtpHost;
                        smtpClient.Credentials = new NetworkCredential(SmtpUsername, SmtpPassword);
                        smtpClient.EnableSsl = SSL;
                        smtpClient.Port = SmtpPort;
                        smtpClient.Send(mailMessage);
                    }
                }
                catch (Exception ex)
                {
                    Exceptions.FileLogger.Log(ex);
                }
            });
        }

        private string GenerateEmailBody(IEnumerable<IGrouping<string, Log>> logs, Usage usage)
        {
            StringWriter stringWriter = new StringWriter();
            using (HtmlTextWriter htmlTextWriter = new HtmlTextWriter(stringWriter))
            {

                htmlTextWriter.AddStyleAttribute(HtmlTextWriterStyle.FontFamily, "Verdana");
                htmlTextWriter.AddStyleAttribute(HtmlTextWriterStyle.Margin, "0 auto");
                htmlTextWriter.AddStyleAttribute(HtmlTextWriterStyle.Width, "800");
                htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.Body);

                htmlTextWriter.AddStyleAttribute(HtmlTextWriterStyle.BackgroundColor, "lightgray");
                htmlTextWriter.AddStyleAttribute(HtmlTextWriterStyle.FontFamily, "Verdana");
                htmlTextWriter.AddStyleAttribute(HtmlTextWriterStyle.Margin, "0 auto");
                htmlTextWriter.AddStyleAttribute(HtmlTextWriterStyle.Width, "500");
                htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.H1);
                htmlTextWriter.WriteEncodedText("apps tracker");
                htmlTextWriter.RenderEndTag(); //endtag H1

                htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.H2);
                htmlTextWriter.WriteEncodedText(string.Format("Email report for {0} - from {1} to {2}.", _dateLast.ToShortDateString(), _dateLast.ToShortTimeString(), _dateNow.ToShortTimeString()));
                htmlTextWriter.RenderEndTag(); //endtag H2

                htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.H3);
                htmlTextWriter.WriteEncodedText(string.Format("User: {0}, logged in since: {1} - duration: {2}",
                    Globals.UserName, usage.UsageStart.ToShortTimeString(), usage.Duration.ToString("hh\\:mm\\:ss")));
                htmlTextWriter.RenderEndTag(); //endtag H3

                htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.H4);
                htmlTextWriter.WriteEncodedText("Applications used:");
                htmlTextWriter.RenderEndTag(); //endtag H4


                htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.Ol);
                foreach (var grp in logs)
                {
                    htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.Li);
                    htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.H4);
                    htmlTextWriter.WriteEncodedText(string.Format("{0}, total logged time: {1}.", grp.Key, new TimeSpan(grp.Sum(g => g.Duration)).ToString("hh\\:mm\\:ss")));
                    htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.H4);
                    htmlTextWriter.WriteEncodedText("Windows opened:");
                    htmlTextWriter.RenderEndTag(); //endtag H3 Windows used
                    htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.Ul);

                    var windowGrp = grp.GroupBy(l => l.WindowTitle);

                    foreach (var window in windowGrp)
                    {
                        htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.Li);
                        htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.P);
                        htmlTextWriter.Write(string.Format("{0}, total logged time: {1}.", window.Key, new TimeSpan(window.Sum(w => w.Duration)).ToString("hh\\:mm\\:ss")));

                        foreach (var log in window)
                        {
                            if (log.KeystrokesRaw != null)
                            {
                                htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.H4);
                                htmlTextWriter.Write(string.Format("Keystrokes logged on: {0}.", log.DateCreated.ToShortTimeString()));
                                htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.Br);
                                htmlTextWriter.RenderEndTag();  //endtag BR
                                htmlTextWriter.Write(log.Keystrokes);
                                htmlTextWriter.RenderEndTag();     //endtag H4
                            }
                        }

                        htmlTextWriter.RenderEndTag();  //endtag P
                        htmlTextWriter.RenderEndTag();  //endtag LI
                    }
                    htmlTextWriter.RenderEndTag(); //endtag UL
                    htmlTextWriter.RenderEndTag(); //endtag H4
                    htmlTextWriter.RenderEndTag(); //endtag H4

                }
                htmlTextWriter.RenderEndTag();     //endtag Li


                htmlTextWriter.RenderEndTag(); //endtag body
                htmlTextWriter.Flush();
            }
            return stringWriter.ToString();

        }

        private Task<Usage> GetCurrentLoginAsync()
        {
            string usageType = UsageTypes.Login.ToString();
            return Task<Usage>.Run(() => _appsService.GetFiltered<Usage>(u => u.UsageType.UType == usageType
                                                && u.IsCurrent)
                                                .OrderByDescending(u => u.UsageStart)
                                                .First());
        }

        private Task<IEnumerable<IGrouping<string, Log>>> GetLogsByDateAsync()
        {
            _dateNow = DateTime.Now;
            _dateLast = DateTime.Now.AddMilliseconds(-(Interval));

            return Task<IEnumerable<IGrouping<string, Log>>>.Run(() => _appsService.GetFiltered<Log>(l => l.Window.Application.User.UserID == Globals.UserID
                                                                    && l.DateCreated >= _dateLast
                                                                    && l.DateCreated <= _dateNow
                                                                    , l => l.Window.Application
                                                                    , l => l.Screenshots)
                                                                .GroupBy(l => l.Window.Application.Name));
        }

        private Task<string> GetZipFile(IEnumerable<IGrouping<string, Log>> logs)
        {
            return Task<string>.Run(() =>
            {
                string zipFile = "screenshots.zip";
                string reportsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                string screenshotsPath = Path.Combine(reportsPath, "Screenshots");

                try
                {
                    if (!Directory.Exists(screenshotsPath))
                        Directory.CreateDirectory(screenshotsPath);
                }
                catch (Exception ex)
                {
                    Exceptions.FileLogger.Log(ex);
                    return null;
                }

                var screenshots = logs.SelectMany(l => l.Select(lg => lg)).Where(log => log.Screenshots.Count > 0);
                foreach (var log in screenshots)
                {
                    string directoryPath = Screenshots.TrimPath(
                          Path.Combine(new string[] { screenshotsPath, Screenshots.CorrectPath(
                        log.Window.Application.Name), Screenshots.CorrectPath(log.Window.Title) }));


                    if (!Directory.Exists(directoryPath))
                        Directory.CreateDirectory(directoryPath);


                    foreach (var screenshot in log.Screenshots)
                    {
                        try
                        {
                            Screenshots.SaveScreenshotToFile(screenshot, Path.Combine(directoryPath, string.Format("{0}_{1}_{2}_{3}_{4}.jpg", screenshot.Date.Month, screenshot.Date.Day, screenshot.Date.Hour, screenshot.Date.Minute, screenshot.Date.Second)));
                        }
                        catch
                        {

                        }
                    }
                }

                if (File.Exists(Path.Combine(reportsPath, zipFile)))
                    File.Delete(Path.Combine(reportsPath, zipFile));


                ZipFile.CreateFromDirectory(screenshotsPath, Path.Combine(reportsPath, zipFile));
                Directory.Delete(screenshotsPath, true);

                return Path.Combine(reportsPath, zipFile);
            });
        }

        private void StopReporting()
        {
            _timer.Enabled = false;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            if (disposing)
            {
                StopReporting();
                _timer.Elapsed -= timer_Elapsed;
                _timer.Dispose();
            }
            _disposed = true;
        }

        #endregion
    }
}
