﻿#region Licence
/*
  *  Author: Marko Devcic, madevcic@gmail.com
  *  Copyright: Marko Devcic, 2015
  *  Licence: http://creativecommons.org/licenses/by-nc-nd/4.0/
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using AppsTracker.Common.Communication;
using AppsTracker.Domain.Screenshots;
using AppsTracker.Domain.Settings;
using AppsTracker.MVVM;
using AppsTracker.Service;

namespace AppsTracker.ViewModels
{
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public sealed class ScreenshotsViewModel : ViewModelBase
    {
        private const int MAX_FILE_NAME_LENGTH = 245;

        private readonly IScreenshotService screenshotService;
        private readonly IAppSettingsService settingsService;
        private readonly IWindowService windowService;
        private readonly Mediator mediator;


        public override string Title
        {
            get { return "SCREENSHOTS"; }
        }


        private string infoContent;

        public string InfoContent
        {
            get { return infoContent; }
            set
            {
                SetPropertyValue(ref infoContent, string.Empty);
                SetPropertyValue(ref infoContent, value);
            }
        }


        private ScreenshotModel selectedItem;

        public ScreenshotModel SelectedItem
        {
            get { return selectedItem; }
            set { SetPropertyValue(ref selectedItem, value); }
        }

        private readonly AsyncProperty<IEnumerable<ScreenshotModel>> logList;

        public AsyncProperty<IEnumerable<ScreenshotModel>> LogList
        {
            get { return logList; }
        }

        private ICommand openScreenshotViewerCommand;

        public ICommand OpenScreenshotViewerCommand
        {
            get { return openScreenshotViewerCommand ?? (openScreenshotViewerCommand = new DelegateCommand(OpenScreenshotViewer)); }
        }


        private ICommand deleteSelectedScreenshotsCommand;

        public ICommand DeleteSelectedScreenshotsCommand
        {
            get
            {
                return deleteSelectedScreenshotsCommand ?? (deleteSelectedScreenshotsCommand = new DelegateCommandAsync(DeleteSelectedScreenhsots
                    , () => selectedItem != null && selectedItem.Images.Any(s => s.IsSelected)));
            }
        }


        private ICommand saveSelectedScreenshotsCommand;

        public ICommand SaveSelectedScreenshotsCommand
        {
            get
            {
                return saveSelectedScreenshotsCommand ??
                    (saveSelectedScreenshotsCommand = new DelegateCommandAsync(SaveSelectedScreenshots
                    , () => selectedItem != null && selectedItem.Images.Any(s => s.IsSelected)));
            }
        }



        [ImportingConstructor]
        public ScreenshotsViewModel(IAppSettingsService settingsService,
                                    IScreenshotService screenshotService,
                                    IWindowService windowService,
                                    Mediator mediator)
        {
            this.settingsService = settingsService;
            this.screenshotService = screenshotService;
            this.windowService = windowService;
            this.mediator = mediator;

            logList = new TaskObserver<IEnumerable<ScreenshotModel>>(screenshotService.GetAsync, this, OnScreenshotGet);

            this.mediator.Register(MediatorMessages.REFRESH_LOGS, new Action(logList.Reload));
        }


        private void OnScreenshotGet(IEnumerable<ScreenshotModel> screenshots)
        {
            foreach (var screenshot in screenshots)
            {
                foreach (var image in screenshot.Images)
                {
                    image.SetPopupSize(System.Windows.SystemParameters.PrimaryScreenWidth, System.Windows.SystemParameters.PrimaryScreenHeight);
                }
            }
        }


        private void OpenScreenshotViewer(object parameter)
        {
            var collection = parameter as IList;
            if (collection == null)
                return;

            var logs = collection.Cast<ScreenshotModel>();
            var screenshotShell = windowService.GetShell("Screenshot viewer window");
            screenshotShell.ViewArgument = logs.Where(l => l.Images.Count() > 0).SelectMany(l => l.Images);
            screenshotShell.Show();
        }


        private async Task DeleteSelectedScreenhsots()
        {
            if (selectedItem == null)
                return;

            var selectedShots = selectedItem.Images.Where(s => s.IsSelected);
            var count = selectedShots.Count();
            await screenshotService.DeleteScreenshotsAsync(selectedShots);
            InfoContent = $"Deleted {count} " + (count == 1 ? "image" : "images");
            logList.Reload();
        }


        private async Task SaveSelectedScreenshots()
        {
            var selectedLog = selectedItem;
            if (selectedLog == null)
                return;

            var pathBuilder = new StringBuilder();
            Working = true;
            var selectedShots = selectedLog.Images.Where(s => s.IsSelected);
            try
            {
                foreach (var shot in selectedShots)
                {
                    await SaveToFileAsync(pathBuilder, selectedLog, shot);
                }
                var count = selectedShots.Count();
                InfoContent = $"Saved {count} " + (count == 1 ? "image" : "images");
            }
            catch (IOException fail)
            {
                windowService.ShowMessage(fail);
            }
            finally
            {
                Working = false;
            }
        }


        private async Task SaveToFileAsync(StringBuilder path, ScreenshotModel screenshot, Image image)
        {
            path.Append(screenshot.AppName);
            path.Append("_");
            path.Append(screenshot.WindowTitle);
            path.Append("_");
            path.Append(image.GetHashCode());
            path.Append(".jpg");
            string folderPath;

            if (Directory.Exists(settingsService.Settings.DefaultScreenshotSavePath))
                folderPath = Path.Combine(settingsService.Settings.DefaultScreenshotSavePath, CorrectPath(path.ToString()));
            else
                folderPath = CorrectPath(path.ToString());

            folderPath = TrimPath(folderPath);
            await SaveScreenshot(image.Screensht, folderPath);
        }

        private string CorrectPath(string path)
        {
            string newTitle = path;
            char[] illegalChars = new char[] { '<', '>', ':', '"', '\\', '/', '|', '?', '*', '0' };
            if (path.IndexOfAny(illegalChars) >= 0)
            {
                foreach (var chr in illegalChars)
                {
                    if (newTitle.Contains(chr))
                    {
                        while (newTitle.Contains(chr))
                        {
                            newTitle = newTitle.Remove(newTitle.IndexOf(chr), 1);
                        }
                    }
                }
            }
            char[] charArray = newTitle.ToArray();
            foreach (var chr in charArray)
            {
                int i = chr;
                if (i >= 1 && i <= 31) newTitle = newTitle.Remove(newTitle.IndexOf(chr), 1);
            }

            return newTitle;
        }

        private string TrimPath(string path)
        {
            var extension = Path.GetExtension(path);
            var pathNoExtension = path.Remove(path.Length - extension.Length - 1, extension.Length + 1);
            if (pathNoExtension.Length >= MAX_FILE_NAME_LENGTH)
            {
                while (pathNoExtension.Length >= MAX_FILE_NAME_LENGTH)
                {
                    pathNoExtension = pathNoExtension.Remove(pathNoExtension.Length - 1, 1);
                }
            }
            return pathNoExtension + extension;
        }

        private async Task SaveScreenshot(byte[] image, string path)
        {
            ImageCodecInfo jgpEncoder = GetEncoder(ImageFormat.Jpeg);

            using (FileStream fileStream = File.Open(path, FileMode.OpenOrCreate))
            {
                await fileStream.WriteAsync(image, 0, image.Length);
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}
