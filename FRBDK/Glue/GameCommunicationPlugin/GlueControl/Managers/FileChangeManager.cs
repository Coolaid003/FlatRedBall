﻿using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using GameCommunicationPlugin.GlueControl.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameCommunicationPlugin.GlueControl.Managers
{


    class FileChangeManager 
    {
        string[] copiedExtensions = new[]
        {
            "csv",
            "txt",
            "png",
            "tmx",
            "tsx",
            "bmp",
            "png",
            "achx",
            "emix",
            "json",
            "xnb"
        };
        private Action<string> _output;
        private RefreshManager _refreshManager;
        CompilerViewModel viewModel;

        public FileChangeManager(Action<string> output, CompilerViewModel viewModel, RefreshManager refreshManager)
        {
            this._output = output;
            _refreshManager = refreshManager;
            this.viewModel = viewModel;
        }

        public void HandleFileChanged(string fileName)
        {
            // If a file changed, always copy it over - why only do so if we're in edit mode?

            var extension = FileManager.GetExtension(fileName);
            var shouldCopy = copiedExtensions.Contains(extension);

            if (shouldCopy)
            {
                GlueCommands.Self.ProjectCommands.CopyToBuildFolder(fileName);

            }

            _refreshManager.HandleFileChanged(fileName);
        }


        private void OutputSuccessOrFailure(bool succeeded)
        {
            if (succeeded)
            {
                _output($"{DateTime.Now.ToLongTimeString()} Build succeeded");
            }
            else
            {
                _output($"{DateTime.Now.ToLongTimeString()} Build failed");
            }
        }
    }
}