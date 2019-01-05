﻿using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Glue.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.RuntimeObjects.File
{
    public class NodeNetworkRuntimeFileManager : RuntimeFileManager
    {
        public override void Activity(ICollection<LoadedFile> allFileObjects)
        {
        }

        public override bool AddToManagers(LoadedFile loadedFile)
        {
            return false;
        }

        public override object CreateEmptyObjectMatchingArgumentType(object originalObject)
        {
            if (originalObject is NodeNetwork)
            {
                return new NodeNetwork();
            }
            return null;
        }

        public override bool DestroyRuntimeObject(object runtimeObject)
        {
            return false;
        }

        public override void RemoveFromManagers(ICollection<LoadedFile> allFileObjects)
        {
        }

        public override bool TryDestroy(LoadedFile runtimeFileObject, ICollection<LoadedFile> allFileObjects)
        {
            return false;
        }

        public override object TryGetCombinedObjectByName(string name)
        {
            return null;
        }

        public override bool TryHandleRefreshFile(FilePath fileName, List<LoadedFile> allFileObjects)
        {
            return false;
        }

        protected override object Load(FilePath filePath)
        {
            return null;
        }
    }
}
