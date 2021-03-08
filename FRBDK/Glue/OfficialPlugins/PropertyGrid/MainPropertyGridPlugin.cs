﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using WpfDataUi;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using OfficialPluginsCore.PropertyGrid.Views;
using OfficialPluginsCore.PropertyGrid.ViewModels;

namespace OfficialPlugins.VariableDisplay
{
    [Export(typeof(PluginBase))]
    public class MainPropertyGridPlugin : EmbeddedPlugin
    {
        #region Fields

        DataUiGrid settingsGrid;
        VariableView variableGrid;

        VariableViewModel variableViewModel;

        PluginTab settingsTab;
        PluginTab2 variableTab;

        const bool showSettings = false;

        #endregion

        public override void StartUp()
        {
            this.ReactToItemSelectHandler += HandleItemSelect;

            this.ReactToLoadedGlux += HandleLoadedGlux;

            this.ReactToChangedPropertyHandler += HandleRefreshProperties;

        }

        private void HandleLoadedGlux()
        {
            HandleItemSelect(GlueState.Self.CurrentTreeNode);
        }

        private void HandleRefreshProperties(string changedMember, object oldValue)
        {
            if (this.variableGrid != null)
            {
                RefreshLogic.RefreshGrid(variableGrid.DataUiGrid);
            }
        }

        //private void HandleRefreshProperties()
        //{
        //    if(this.variableGrid != null)
        //    {
        //        VariableShowingLogic.RefreshGrid(variableGrid);
        //    }
        //}

        private void HandleItemSelect(System.Windows.Forms.TreeNode selectedTreeNode)
        {
            if(GlueState.Self.CurrentNamedObjectSave != null)
            {
                HandleNamedObjectSelect(GlueState.Self.CurrentNamedObjectSave);
            }
            else if(GlueState.Self.CurrentStateSave != null || GlueState.Self.CurrentStateSaveCategory != null)
            {
                // For now we don't handle showing states, so let's hide it so the user doens't think
                // they are editing states
                RemoveTab(variableTab);
            }
            else if(GlueState.Self.CurrentElement != null && 
                (selectedTreeNode.IsRootCustomVariablesNode() 
                // Feb 18, 2021 - It's annoying to have to select the Variables
                // node. The user should be able to see variables just by selecting
                // the entity itself.
                || selectedTreeNode.IsElementNode()))
            {
                ShowVariablesForCurrentElement();
            }
            else
            {
                RemoveTab(variableTab);

                if (showSettings)
                {
                    RemoveTab(settingsTab);
                }
            }
        }

        private void ShowVariablesForCurrentElement()
        {
            //if (showSettings)
            //{
            //    AddOrShowSettingsGrid();
            //    settingsGrid.Instance = GlueState.Self.CurrentElement;
            //}

            AddOrShowVariableGrid();
            // we can always add new variables to entities....right?
            variableViewModel.CanAddVariable = true;
            variableGrid.DataUiGrid.Instance = GlueState.Self.CurrentElement;
            ElementVariableShowingLogic.UpdateShownVariables(variableGrid.DataUiGrid, GlueState.Self.CurrentElement);
        }

        private void HandleNamedObjectSelect(NamedObjectSave namedObject)
        {

            //if (showSettings)
            //{
            //    AddOrShowSettingsGrid();
            //    settingsGrid.Instance = namedObject;
            //}


            // If we are showing a NOS that comes from a file, don't show the grid.
            // Old Glue used to show the grid, but this introduces problems:
            // 1. This means that the variable showing code has to look at the file to
            //    get current values, rather than just at the ATI. This means we need new
            //    functionality in the plugin class for pulling values from files. 
            // 2. The plugin class has to do some kind of intelligent caching to prevent
            //    hitting the disk for every property (which would be slow). 
            // 3. The plugin will also have to respond to file changes and refresh the grid.
            // 4. This may confuse users who are expecting the changes in Glue to modify the original
            //    file, instead of them understanding that Glue overrides the file. 
            // I think I'm going to keep it simple and only show the grid if it doesn't come from file:
            // Update Jan 24, 2019
            // I thought about this problem for a bit and realized that this can be solved in two steps.
            // 1. The problem is that if we show the values that come from the AssetTypeInfo on a from-file
            //    object, the default values in the ATI may not match the values of the object in the file, which
            //    has mislead developers in the past. That is why I opted to just not show a property grid at all.
            //    However, if we just didn't show any defaults at all, that would solve the problem! To do this, we
            //    just need to take the AssetTypeInfo, clone it, and make the default values for all variables be null.
            //    This should result in a property grid that shows the variables with blank boxes/UI so that the user can
            //    fill it in.
            // 2. Plugins could be given the copied ATI to fill in with values from-file. If plugins don't, then the values
            //    would be blank, but if the plugin does want to, then the values from-file would show up.
            // I'm not going to implement this solution just yet, but I thought of how it could be done and thought it would
            // be good to document it here for future reference.
            // Update August 16, 2020
            // 1. I like the solution above to avoid confusion; but there are times when an object has properties which don't
            //    come from file - like the X/Y values of a TileMap. Therefore, some properties should have defaults that stick
            //    around. Eventually this probably means a new property on the VariableDefinition object.
            var isFile = namedObject.SourceType == SourceType.File;

            var ati = namedObject.GetAssetTypeInfo();

            if(isFile && ati != null)
            {
                ati = FlatRedBall.IO.FileManager.CloneObject<AssetTypeInfo>(ati);
                foreach(var variable in ati.VariableDefinitions)
                {
                    variable.DefaultValue = null;
                }
            }

            AddOrShowVariableGrid();
            // can't add variables on the instance:
            variableViewModel.CanAddVariable = false;
            variableGrid.DataUiGrid.Instance = namedObject;
            variableGrid.Visibility = System.Windows.Visibility.Visible;

            NamedObjectVariableShowingLogic.UpdateShownVariables(variableGrid.DataUiGrid, namedObject,
                GlueState.Self.CurrentElement, ati);
        }

        //private void AddOrShowSettingsGrid()
        //{
        //    if(settingsGrid == null)
        //    {

        //        settingsGrid = new DataUiGrid();
        //        settingsTab = this.AddToTab(PluginManager.CenterTab, settingsGrid, "Settings");
        //        settingsTab.DrawX = false;
        //    }
        //    else
        //    {
        //        this.ShowTab(settingsTab);
        //    }
        //}

        private void AddOrShowVariableGrid()
        {
            if(variableGrid == null)
            {
                variableGrid = new VariableView();

                variableViewModel = new VariableViewModel();
                variableGrid.DataContext = variableViewModel;

                variableTab = this.CreateTab(variableGrid, "Variables", TabLocation.Center);
                //this.ShowTab(variableTab, TabLocation.Center);
                
                //variableTab = this.AddToTab(tabControl, variableGrid, "Variables");
                //variableTab.DrawX = false;

                // let's make this the first item and have it be focused:
                //tabControl.SelectedTab = variableTab;
                GlueCommands.Self.DialogCommands.FocusTab("Variables");
                // This makes it the last tab clicked, which gives it priority:
                //variableTab.LastTimeClicked = DateTime.Now;
            }
            else
            {
                this.ShowTab(variableTab);
            }
        }
    }
}
