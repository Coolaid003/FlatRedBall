﻿using CompilerLibrary.ViewModels;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GameCommunicationPlugin.GlueControl.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace GameCommunicationPlugin.GlueControl.Views
{
    /// <summary>
    /// Interaction logic for WhileRunningView.xaml
    /// </summary>
    public partial class WhileRunningView : UserControl
    {
        #region Properties

        public CompilerViewModel ViewModel => DataContext as CompilerViewModel;

        #endregion

        #region Events

        public event EventHandler StopClicked;
        public event EventHandler RestartGameClicked;
        public event EventHandler RestartGameCurrentScreenClicked;
        public event EventHandler RestartScreenClicked;
        public event EventHandler PauseClicked;
        public event EventHandler AdvanceOneFrameClicked;
        public event EventHandler UnpauseClicked;

        #endregion

        public WhileRunningView()
        {
            InitializeComponent();
        }

        private void HandleStopClicked(object sender, RoutedEventArgs e)
        {
            StopClicked?.Invoke(this, null);
        }

        private void HandleRestartGameCurrentScreenClicked(object sender, RoutedEventArgs e)
        {
            if(ViewModel.IsGenerateGlueControlManagerInGame1Checked)
            {
                RestartGameCurrentScreenClicked?.Invoke(this, null);
            }
            else
            {
                RestartGameClicked?.Invoke(this, null);

                //ShowMessageAboutGenerateGame1();
            }
        }

        private void HandleRestartScreenClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsGenerateGlueControlManagerInGame1Checked)
            {
                RestartScreenClicked?.Invoke(this, null);
            }
            else
            {
                ShowMessageAboutGenerateGame1();
            }
        }

        private void HandlePauseClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsGenerateGlueControlManagerInGame1Checked)
            {
                PauseClicked?.Invoke(this, null);
            }
            else
            {
                ShowMessageAboutGenerateGame1();
            }
        }

        private void HandleAdvanceOneFrameClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsGenerateGlueControlManagerInGame1Checked)
            {
                AdvanceOneFrameClicked?.Invoke(this, null);
            }
            else
            {
                ShowMessageAboutGenerateGame1();
            }
        }

        private void HandleUnpauseClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsGenerateGlueControlManagerInGame1Checked)
            {
                UnpauseClicked?.Invoke(this, null);
            }
            else
            {
                ShowMessageAboutGenerateGame1();
            }
        }

        private void ShowMessageAboutGenerateGame1()
        {
            GlueCommands.Self.DialogCommands.ShowMessageBox(Localization.Texts.GlueCannotCommunicateWithGame);
        }

        private void SpeedDecreaseClicked(object sender, RoutedEventArgs e)
        {
            ViewModel.DecreaseGameSpeed();
        }

        private void SpeedIncreaseClicked(object sender, RoutedEventArgs e)
        {
            ViewModel.IncreaseGameSpeed();
        }
    }
}
