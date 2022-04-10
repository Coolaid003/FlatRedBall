﻿using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.Compiler.CodeGeneration
{
    public static class EmbeddedCodeManager
    {
        static string glueControlFolder => GlueState.Self.CurrentGlueProjectDirectory + "GlueControl/";

        public static void EmbedAll(bool fullyGenerate)
        {
            GlueControlCodeGenerator.GenerateFull = fullyGenerate;

            SaveEmbeddedFile("CommandReceiver.cs");

            SaveEmbeddedFile("Dtos.cs");

            SaveEmbeddedFile("Editing.CameraLogic.cs");
            SaveEmbeddedFile("Editing.CopyPasteManager.cs");
            SaveEmbeddedFile("Editing.EditingManager.cs");
            SaveEmbeddedFile("Editing.EditorVisuals.cs");
            SaveEmbeddedFile("Editing.Guides.cs");

            SaveEmbeddedFile("Editing.Managers.GenerateCodeCommands.cs");
            SaveEmbeddedFile("Editing.Managers.GlueCommands.cs");
            SaveEmbeddedFile("Editing.Managers.GlueCommandsStateBase.cs");
            SaveEmbeddedFile("Editing.Managers.GlueState.cs");
            SaveEmbeddedFile("Editing.Managers.GluxCommands.cs");


            SaveEmbeddedFile("Editing.Markers.SelectionMarker.cs");
            SaveEmbeddedFile("Editing.Markers.TileShapeCollectionMarker.cs");

            SaveEmbeddedFile("Editing.MoveObjectToContainerLogic.cs");

            SaveEmbeddedFile("Editing.SelectionLogic.cs");

            SaveEmbeddedFile("Editing.VariableAssignmentLogic.cs");

            SaveEmbeddedFile("Editing.Visuals.Arrow.cs");

            SaveEmbeddedFile("Forms.ObjectCreationWindow.cs");

            SaveEmbeddedFile("GlueControlManager.cs");

            SaveEmbeddedFile("InstanceLogic.cs");


            SaveEmbeddedFile("Models.CustomVariable.cs");
            SaveEmbeddedFile("Models.GlueElement.cs");
            SaveEmbeddedFile("Models.IElementExtensionMethods.cs");
            SaveEmbeddedFile("Models.NamedObjectSave.cs");
            SaveEmbeddedFile("Models.StateSave.cs");
            SaveEmbeddedFile("Models.StateSaveCategory.cs");

            SaveEmbeddedFile("Runtime.DynamicEntity.cs");
            // This was a typo in old projects:
            RemoveEmbeddedFile("Runtime/DynamicEntitys.Generated.cs");

            SaveEmbeddedFile("Screens.EntityViewingScreen.cs");

        }

        private static void RemoveEmbeddedFile(string relativePath)
        {
            FilePath absoluteFile = GlueState.Self.CurrentGlueProjectDirectory + "GlueControl/" + relativePath;

            GlueCommands.Self.ProjectCommands.RemoveFromProjects(absoluteFile);
        }

        private static void SaveEmbeddedFile(string resourcePath)
        {
            var split = resourcePath.Split(".").ToArray();
            split = split.Take(split.Length - 1).ToArray(); // take off the .cs
            var combined = string.Join('/', split) + ".Generated.cs";
            var relativeDestinationFilePath = combined;

            var prefix = "OfficialPlugins.Compiler.Embedded.";
            string glueControlManagerCode = GlueControlCodeGenerator.GetEmbeddedStringContents(prefix + resourcePath);
            FilePath destinationFilePath = glueControlFolder + relativeDestinationFilePath;
            GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(destinationFilePath);
            GlueCommands.Self.TryMultipleTimes(() => System.IO.File.WriteAllText(destinationFilePath.FullPath, glueControlManagerCode));
        }
    }
}
