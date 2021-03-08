﻿using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SetVariable;
using Glue;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Windows.Forms;

namespace OfficialPluginsCore.PropertiesTabOldPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPropertiesTabOldPlugin : EmbeddedPlugin
    {
        //PluginTab pluginTab;
        public System.Windows.Forms.PropertyGrid PropertyGrid;
        private System.Windows.Forms.ContextMenuStrip PropertyGridContextMenu;


        public override void StartUp()
        {
            CreatePropertyGrid();

            var tab = PluginManager.CenterTab;
            this.AddToTab(PropertyGrid, "Properties", TabLocation.Center);
            this.ReactToItemSelectHandler += HandleItemSelected;

            HandleItemSelected(null);
        }

        private void HandleItemSelected(TreeNode selectedTreeNode)
        {
            var selectedObject = selectedTreeNode?.Tag;

            var shouldShow = selectedObject is ScreenSave ||
                selectedObject is EntitySave ||
                selectedObject is StateSaveCategory ||
                selectedObject is StateSave ||
                selectedObject is CustomVariable ||
                selectedObject is NamedObjectSave ||
                selectedObject is EventResponseSave ||
                selectedObject is ReferencedFileSave;

            if(shouldShow)
            {
                this.AddToTab(PropertyGrid, "Properties", TabLocation.Center);
            }
            else
            {
                base.RemoveAllTabs();
            }
        }

        private void CreatePropertyGrid()
        {
            this.PropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.PropertyGridContextMenu = new System.Windows.Forms.ContextMenuStrip(MainGlueWindow.Self.Components);

            // 
            // PropertyGrid
            // 
            this.PropertyGrid.CategoryForeColor = System.Drawing.SystemColors.InactiveCaptionText;
            this.PropertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PropertyGrid.LineColor = System.Drawing.SystemColors.ControlDark;
            this.PropertyGrid.Location = new System.Drawing.Point(0, 0);
            this.PropertyGrid.Margin = new System.Windows.Forms.Padding(0);
            this.PropertyGrid.Name = "PropertyGrid";
            this.PropertyGrid.PropertySort = System.Windows.Forms.PropertySort.Categorized;
            this.PropertyGrid.Size = new System.Drawing.Size(534, 546);
            this.PropertyGrid.TabIndex = 2;
            this.PropertyGrid.ToolbarVisible = false;
            this.PropertyGrid.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.propertyGrid1_PropertyValueChanged);
            this.PropertyGrid.SelectedGridItemChanged += new System.Windows.Forms.SelectedGridItemChangedEventHandler(this.PropertyGrid_SelectedGridItemChanged);

            // 
            // PropertyGridContextMenu
            // 
            this.PropertyGridContextMenu.Name = "PropertyGridContextMenu";
            this.PropertyGridContextMenu.Size = new System.Drawing.Size(61, 4);

            MainGlueWindow.Self.PropertyGrid = this.PropertyGrid;

            PropertyGridHelper.Initialize(PropertyGrid);

        }

        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            EditorObjects.IoC.Container.Get<SetPropertyManager>().PropertyValueChanged(e, this.PropertyGrid);

        }

        private void PropertyGrid_SelectedGridItemChanged(object sender, SelectedGridItemChangedEventArgs e)
        {
            PropertyGridRightClickHelper.ReactToRightClick();
        }
    }
}
