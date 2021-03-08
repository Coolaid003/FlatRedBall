﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins;
using OfficialPlugins.BuiltFileSizeInspector.ViewModels;
using OfficialPlugins.BuiltFileSizeInspector.Views;

namespace OfficialPlugins.BuiltFileSizeInspector
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {
        SizeInspectorControl mainControl;


        public override string FriendlyName
        {
            get { return "Built File Size Inspector"; }
        }

        public override Version Version
        {
            get { return new Version(1,0); }
        }

        public override void StartUp()
        {
            base.AddMenuItemTo("View Built Project Sizes", HandleViewBuiltProjectSizes, "Plugins");
        }

        private void HandleViewBuiltProjectSizes(object sender, EventArgs e)
        {
            if(mainControl == null)
            {
                mainControl = new SizeInspectorControl();
                mainControl.DataContext = new BuiltFileSizeViewModel();

            }
            this.AddToTab(mainControl, "Built File Size", TabLocation.Left);
        }

        public override bool ShutDown(FlatRedBall.Glue.Plugins.Interfaces.PluginShutDownReason shutDownReason)
        {
            return true;
        }
    }
}
