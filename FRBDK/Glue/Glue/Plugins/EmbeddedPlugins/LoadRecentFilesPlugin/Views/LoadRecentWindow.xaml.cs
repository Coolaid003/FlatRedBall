﻿using FlatRedBall.Glue.Plugins.EmbeddedPlugins.LoadRecentFilesPlugin.ViewModels;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GlueFormsCore.Extensions;
using System;
using System.Windows;
using System.Windows.Input;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.LoadRecentFilesPlugin.Views
{
    /// <summary>
    /// Interaction logic for LoadRecentWindow.xaml
    /// </summary>
    public partial class LoadRecentWindow : Window
    {
        LoadRecentViewModel ViewModel => DataContext as LoadRecentViewModel;

        public LoadRecentWindow()
        {
            InitializeComponent();

            this.Loaded += HandleLoaded;
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            GlueCommands.Self.DialogCommands.MoveToCursor(this);

            this.SearchBar.FocusTextBox();
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedItem != null)
            {
                this.DialogResult = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void SearchBar_ClearSearchButtonClicked()
        {
            ViewModel.SearchBoxText = String.Empty;
        }

        private void SearchBar_ArrowKeyPushed(Key key)
        {

        }

        private void SearchBar_EnterPressed()
        {
            if (ViewModel.SelectedItem != null)
            {
                DialogResult = true;
            }
        }

        private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.SelectedItem != null)
            {
                DialogResult = true;
            }
        }

        private void SearchBar_EscapePressed()
        {
            DialogResult = false;
        }

        private void ListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Delete)
            {
                var selectedItem = ViewModel.SelectedItem;

                if(selectedItem != null)
                {
                    selectedItem.HandleRemoveClicked();
                }
            }
        }
    }
}
