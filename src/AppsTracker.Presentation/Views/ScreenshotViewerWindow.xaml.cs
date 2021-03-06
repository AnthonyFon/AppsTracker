﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Input;
using AppsTracker.Data.Models;
using AppsTracker.Domain.Screenshots;
using AppsTracker.ViewModels;
using AppsTracker.Widgets;

namespace AppsTracker.Views
{
    [Export(typeof(IShell))]
    [ExportMetadata("ShellUse", "Screenshot viewer window")]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class ScreenshotViewerWindow : System.Windows.Window, IShell
    {
        private readonly ScreenshotViewerViewModel viewModel;

        [ImportingConstructor]
        public ScreenshotViewerWindow(ScreenshotViewerViewModel viewModel)
        {
            InitializeComponent();
            this.viewModel = viewModel;
            this.DataContext = viewModel;
            this.WindowState = System.Windows.WindowState.Maximized;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void ChangeViewButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            WindowState = System.Windows.WindowState.Normal;
        }

        private void MinimizeButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            WindowState = System.Windows.WindowState.Minimized;
        }

        private void MaximizeButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            WindowState = System.Windows.WindowState.Maximized;
        }

        public object ViewArgument
        {
            get
            {
                return viewModel.ScreenshotCollection;
            }
            set
            {
                viewModel.ScreenshotCollection = (IEnumerable<Image>)value;
                scViewer.lbImages.SelectedIndex = 0;
            }
        }
    }
}
