﻿using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using OfficialPlugins.ErrorPlugin.Logic;
using OfficialPlugins.ErrorPlugin.ViewModels;
using OfficialPlugins.ErrorPlugin.Views;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.ErrorPlugin
{
    [Export(typeof(PluginBase))]
    public class MainErrorPlugin : PluginBase
    {
        #region Fields/Properties

        public override string FriendlyName => "Error Window Plugin";
        public override Version Version => new Version(1, 0);

        ErrorListViewModel errorListViewModel;
        PluginTab2 tab;
        ErrorWindow control;

        public bool HasErrors => errorListViewModel?.Errors.Count > 0;

        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            EditorObjects.IoC.Container.Get<List<IErrorReporter>>()
                .Add(new ErrorCreateRemoveLogic());

            control = new ErrorWindow();
            tab = AddToTab(control, "Errors", TabLocation.Bottom);

            errorListViewModel = GlueState.Self.ErrorList;
            errorListViewModel.Errors.CollectionChanged += HandleErrorsCollectionChanged;
            errorListViewModel.RefreshClicked += HandleRefreshClicked;

            control.DataContext = errorListViewModel;


            this.ReactToLoadedGlux += HandleLoadedGlux;
            this.ReactToFileChangeHandler += HandleFileChanged;
            this.ReactToFileRemoved += HandleFileRemoved;
            this.ReactToUnloadedGlux += HandleUnloadedGlux;
            this.ReactToFileReadError += HandleFileReadError ;

            RefreshCommands.RefreshErrorsAction = () => RefreshLogic.RefreshAllErrors(errorListViewModel);
        }

        private void HandleRefreshClicked(object sender, EventArgs e)
        {
            RefreshLogic.RefreshAllErrors(errorListViewModel);
        }

        private void HandleErrorsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            GlueCommands.Self.DoOnUiThread(() =>
            {
                RefreshTabText();


                if(e.NewItems?.Count > 0)
                {
                    FocusTab();
                }
            });

            // If I don't do this the list shows an extra item. Not sure why...
            //control.ForceRefreshErrors();
        }

        private void HandleFileReadError(FilePath fileName, GeneralResponse response)
        {
            //RefreshLogic.HandleFileChange(fileName, errorListViewModel);
            // this could get kicked off when tracking dependencies for other errors, so we want to be specific to prevent infinite loops:
            RefreshLogic.HandleFileReadError(fileName, errorListViewModel);
        }

        private void HandleFileRemoved(IElement container, ReferencedFileSave removedFile)
        {
            RefreshLogic.HandleReferencedFileRemoved(removedFile, errorListViewModel);
        }

        private void HandleFileChanged(string fileName)
        {
            RefreshLogic.HandleFileChange(fileName, errorListViewModel);
        }

        private void RefreshTabText()
        {
            var numberOfErrors = errorListViewModel.Errors.Count;
            var tabText = $"Errors ({numberOfErrors})";

            if(tab.Title != tabText)
            {
                tab.Title = tabText;
            }
        }

        private void HandleLoadedGlux()
        {
            RefreshLogic.RefreshAllErrors(errorListViewModel);
        }

        private void HandleUnloadedGlux()
        {
            lock (GlueState.ErrorListSyncLock)
            {
                errorListViewModel.Errors.Clear();
            }
        }
    }
}
