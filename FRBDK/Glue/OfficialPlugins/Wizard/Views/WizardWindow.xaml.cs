﻿using OfficialPluginsCore.Wizard.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OfficialPluginsCore.Wizard.Views
{
    /// <summary>
    /// Interaction logic for WizardWindow.xaml
    /// </summary>
    public partial class WizardWindow : Window
    {
        public WizardViewModel WizardData { get; private set; }

        public event Func<Task> DoneClicked;

        public WizardWindow()
        {
            InitializeComponent();

            var definition = new WizardFormsDefinition();
            definition.CreatePages();

            definition.Start(GridInstance);

            var hasClickedDone = false;

            definition.DoneClicked += async () =>
            {
                if(!hasClickedDone)
                {
                    hasClickedDone = true;
                    GridInstance.Visibility = Visibility.Collapsed;
                    PleaseWaitGrid.Visibility = Visibility.Visible;
                    // This could change:
                    WizardData = definition.ViewModel;
                    PleaseWaitGrid.DataContext = WizardData;
                    // prevent double clicking
                    await DoneClicked();

                    this.DialogResult = true;
                }

            };
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
        }
    }
}
