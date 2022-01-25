﻿using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Math;
using Glue;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using OfficialPlugins.Compiler.CommandSending;
using OfficialPlugins.Compiler.Dtos;
using OfficialPlugins.Compiler.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TMXGlueLib;
using ToolsUtilities;
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace OfficialPluginsCore.Compiler.CommandReceiving
{
    static class CommandReceiver
    {
        static int gamePortNumber;

        #region General Functions

        public static void HandleCommandsFromGame(List<string> commands, int gamePortNumber)
        {
            foreach (var command in commands)
            {
                HandleIndividualCommand(command, gamePortNumber);
            }
        }

        private static void HandleIndividualCommand(string command, int gamePortNumber)
        {
            CommandReceiver.gamePortNumber = gamePortNumber;
            var firstColon = command.IndexOf(":");
            if(firstColon == -1)
            {
                GlueCommands.Self.PrintOutput($"Received unknown command: {command}");
            }
            else
            {
                var action = command.Substring(0, firstColon);
                var data = command.Substring(firstColon + 1);

                TaskManager.Self.AddAsync(() =>
                {
                    switch(action)
                    {
                        case nameof(AddObjectDto):
                            {
                                var addObjectDto = JsonConvert.DeserializeObject<AddObjectDto>(data);
                                var nos = JsonConvert.DeserializeObject<NamedObjectSave>(data);

                                ScreenSave screen = CommandSender.GetCurrentInGameScreen().Result;
                                screen = screen ?? GlueState.Self.CurrentScreenSave;

                                HandleAddObject(addObjectDto, nos, saveRegenAndUpdateUi:true, screen:screen);
                            }

                            break;
                        case nameof(SetVariableDto):
                            HandleSetVariable(JsonConvert.DeserializeObject<SetVariableDto>(data));
                            break;
                        case nameof(SetVariableDtoList):
                            HandleSetVariableDtoList(JsonConvert.DeserializeObject<SetVariableDtoList>(data));
                            break;
                        case nameof(SelectObjectDto):
                            HandleSelectObject(JsonConvert.DeserializeObject<SelectObjectDto>(data));
                            break;
                        case nameof(RemoveObjectDto):
                            HandleRemoveObject(JsonConvert.DeserializeObject<RemoveObjectDto>(data));
                            break;
                        case nameof(ModifyCollisionDto):
                            HandleDto(JsonConvert.DeserializeObject<ModifyCollisionDto>(data));
                            break;
                        case nameof(AddObjectDtoList):
                            var deserializedList = JsonConvert.DeserializeObject<AddObjectDtoList>(data);
                            HandleAddObjectDtoList(deserializedList);
                            break;
                        default:
                            int m = 3;
                            break;
                    }
                },
                $"Processing command of type {action}");

            }
        }

        private static async void HandleAddObjectDtoList(AddObjectDtoList deserializedList)
        {
            ScreenSave screen = await CommandSender.GetCurrentInGameScreen();
            screen = screen ?? GlueState.Self.CurrentScreenSave;

            List<NamedObjectSave> newNamedObjects = new List<NamedObjectSave>();

            foreach (var item in deserializedList.Data)
            {
                var cloneString = JsonConvert.SerializeObject(item);
                var nos = JsonConvert.DeserializeObject<NamedObjectSave>(cloneString);
                newNamedObjects.Add(nos);
                await HandleAddObject(item, nos, saveRegenAndUpdateUi:false, screen:screen);
            }

            GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(screen);

            GlueCommands.Self.DoOnUiThread(() =>
            {
                MainGlueWindow.Self.PropertyGrid.Refresh();
                PropertyGridHelper.UpdateNamedObjectDisplay();
                GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(screen);

                if(newNamedObjects.Count > 0)
                {
                    foreach(var item in deserializedList.Data)
                    {
                        if(item.SelectNewObject)
                        {
                            var objectToSelect = newNamedObjects.FirstOrDefault(item => item.InstanceName == item.InstanceName);
                            if(objectToSelect != null)
                            {
                                GlueState.Self.CurrentNamedObjectSave = objectToSelect;
                                break;
                            }
                        }
                    }
                }
            });

            GlueCommands.Self.GluxCommands.SaveGlux();
        }


        #endregion

        #region Remove Object

        private static async void HandleRemoveObject(RemoveObjectDto removeObjectDto)
        {
            ScreenSave screen = await CommandSender.GetCurrentInGameScreen();
            if(screen != null)
            {
                await TaskManager.Self.AddAsync(() =>
                {
                    var objectsToRemove = removeObjectDto.ObjectNames
                        .Select(objectName => screen.GetNamedObjectRecursively(objectName))
                        .Where(item => item != null)
                        .ToList();

                    GlueCommands.Self.GluxCommands.RemoveNamedObjectListAsync(objectsToRemove);

                }, "Handling removing object from screen");
            }
        }

        #endregion

        #region Add Object (including copy/paste

        private static Task HandleAddObject(AddObjectDto addObjectDto, NamedObjectSave nos, bool saveRegenAndUpdateUi, ScreenSave screen)
        {
            return TaskManager.Self.AddAsync(() =>
            {
                screen = screen ?? GlueState.Self.CurrentScreenSave;
                GlueElement element = screen;
                

                var listToAddTo = ObjectFinder.Self.GetDefaultListToContain(addObjectDto, screen);

                string newName = GetNewName(element, addObjectDto);
                var oldName = addObjectDto.InstanceName;


                nos.InstanceName = newName;

                foreach (var variable in nos.InstructionSaves)
                {
                    object value = variable.Value;
                    var typeName = variable.Type;
                    value = ConvertVariable(value, ref typeName, variable.Member, nos, element);
                    variable.Type = typeName;
                    variable.Value = value;
                }

                #region Send the new name back to the game so the game uses the actual Glue name rather than the AutoName
                // do this before adding the NOS to Glue since adding the NOS to Glue results in an AddToList command
                // sent to the game, and we want the right name before the AddToList command
                var data = new GlueVariableSetData();
                data.Type = "string";
                data.VariableValue = newName;
                data.VariableName = "this." + oldName + ".Name";
                data.InstanceOwnerGameType = addObjectDto.ElementNameGame;

                // At this point the element does not contain the new nos
                // We want it to, but we have to send the DTO to the game before
                // calling GluxCommands.AddNamedObjectTo
                // We can make a copy:
                var serialized = JsonConvert.SerializeObject(element);

                if(element is ScreenSave)
                {
                    data.ScreenSave = JsonConvert.DeserializeObject<ScreenSave>(serialized);
                }
                else
                {
                    data.EntitySave = JsonConvert.DeserializeObject<EntitySave>(serialized);
                }

                data.GlueElement.NamedObjects.Add(nos);


                var renameResponseTask = CommandSender.Send<GlueVariableSetDataResponse>(data);
                renameResponseTask.Wait();
                var result = renameResponseTask.Result?.Data;

                if(result == null || !string.IsNullOrEmpty(result.Exception) || result.WasVariableAssigned == false)
                {
                    int m = 3;
                }


                #endregion

                GlueCommands.Self.DoOnUiThread(() =>
                {
                    RefreshManager.Self.IgnoreNextObjectAdd = true;
                    RefreshManager.Self.IgnoreNextObjectSelect = true;

                    // This will result in Glue selecting the new object. When GView copy/pastes, it keeps the
                    // old object selected. Glue shouldn't assume this is the case, because in the future new instances
                    // may be added without copy/paste, and those would be selected. Therefore, we'll rely on the game to 
                    // tell us to select something different...
                    GlueCommands.Self.GluxCommands.AddNamedObjectTo(nos, element, listToAddTo, 
                        selectNewNos: false, // The caller of this is responsible for selection, so that selection goes faster
                        performSaveAndGenerateCode:saveRegenAndUpdateUi, 
                        updateUi: saveRegenAndUpdateUi);
                });

                //RefreshManager.Self.HandleNamedObjectValueChanged(nameof(deserializedNos.InstanceName), oldName, deserializedNos);

            }, "Adding NOS");
        }

        private static string GetNewName(GlueElement glueElement, AddObjectDto addObjectDto)
        {
            string newName = addObjectDto.CopyOriginalName;

            if(string.IsNullOrEmpty(newName))
            {
                if(addObjectDto.SourceClassType.Contains('.'))
                {
                    var suffix = addObjectDto.SourceClassType.Substring(addObjectDto.SourceClassType.LastIndexOf('.') + 1);
                    newName = suffix + "1";
                }
                else
                {
                    var lastSlash = addObjectDto.SourceClassType.LastIndexOf("\\");
                    newName = addObjectDto.SourceClassType.Substring(lastSlash + 1) + "1";
                }
            }
            while (glueElement.GetNamedObjectRecursively(newName) != null)
            {
                newName = StringFunctions.IncrementNumberAtEnd(newName);
            }

            return newName;
        }

        #endregion

        #region Set Variable

        private static async Task HandleSetVariable(SetVariableDto setVariableDto, bool regenerateAndSave = true, bool sendBackToGame = true)
        {

            await TaskManager.Self.AddAsync(() =>
            {
                var type = string.Join('\\', setVariableDto.InstanceOwner.Split('.').Skip(1));

                var element = ObjectFinder.Self.GetElement(type);

                if(setVariableDto.ObjectCreationSource == "Tiled")
                {
                    HandleVariableSetOnTiledObject(setVariableDto);
                }
                else
                {
                    var nos = element.GetNamedObjectRecursively(setVariableDto.ObjectName);

                    if (nos != null)
                    {
                        object value = setVariableDto.VariableValue;
                        var typeName = setVariableDto.Type;

                        if(string.IsNullOrEmpty(typeName))
                        {
                            throw new InvalidOperationException($"Variable {setVariableDto.VariableName} came from glue with a value of {typeName} but didn't have a type");
                        }

                        value = ConvertVariable(value, ref typeName, setVariableDto.VariableName, nos, element);

                        // Calling nos.SetVariable rather than going through the GlueCommands prevents the PluginManager from being notified of the change.
                        // We want to manually push the change back using a batch command since it will be much faster when receiving a SetVariableDtoList
                        nos.SetVariable(setVariableDto.VariableName, value);

                        if(sendBackToGame)
                        {
                            // Send this back to the game so the game. When the game receives this, it will store it in
                            // a list and will re-run the commands as necessary (such as whenever a screen is reloaded).
                            GlueCommands.Self.DoOnUiThread(() =>
                                RefreshManager.Self.HandleNamedObjectValueChanged(setVariableDto.VariableName, null, nos, 
                                // record only - this variable change came from the game, we don't want to re-assign it and wipe other active edits
                                AssignOrRecordOnly.RecordOnly)
                            );
                        }

                        if(regenerateAndSave)
                        {

                            // this may not be the current screen:
                            var nosParent = ObjectFinder.Self.GetElementContaining(nos);

                            GlueCommands.Self.GluxCommands.SaveGlux();
                            GlueCommands.Self.DoOnUiThread(GlueCommands.Self.RefreshCommands.RefreshVariables);
                            GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(nosParent);
                        }

                    }

                }
            }, "Handling set variable from game", 
            // This is going to push the change back to the game, and we don't want to sit and wait for codegen to finish, etc. Do it immediately!
            // Update - if it's asap, then later commands can execute before earlier ones, so we need to still respect fifo. Fifo adds delays but avoids
            // the confusion.
            TaskExecutionPreference.Fifo);
        }

        private static void HandleVariableSetOnTiledObject(SetVariableDto dto)
        {
            var currentScreen = GlueState.Self.CurrentScreenSave;

            // This assumes there's only 1
            var tmxObjectRfs = currentScreen.GetAllReferencedFileSavesRecursively().FirstOrDefault(item => item.Name.ToLowerInvariant().EndsWith(".tmx"));

            FlatRedBall.IO.FilePath tmxFilePath = null;

            if (tmxObjectRfs != null)
            {
                tmxFilePath = GlueCommands.Self.GetAbsoluteFilePath(tmxObjectRfs);
            }

            TiledMapSave tiledMapSave = null;
            MapLayer mapLayer = null;
            if (tmxFilePath?.Exists() == true)
            {
                tiledMapSave = TiledMapSave.FromFile(tmxFilePath.FullPath);
                // assume this for now...
                mapLayer = tiledMapSave.Layers.Find(item => item.Name == "GameplayLayer");
            }

            if(mapLayer != null)
            {
                // check if it's an object layer
                // find the object at the old X, Y, change its position, save the file
            }

            //FlatRedBall.IO.FilePath tmxFilePath;
            //TiledMapSave tiledMapSave;
            //MapLayer mapLayer;
            //GetMapLayer(dto, out collisionTileTypeName, out tmxFilePath, out tiledMapSave, out mapLayer);
        }

        private static async Task HandleSetVariableDtoList(SetVariableDtoList setVariableDtoList)
        {
            HashSet<NamedObjectSave> modifiedObjects = new HashSet<NamedObjectSave>();

            var gameScreenName = await CommandSender.GetScreenName();

            List<GlueVariableSetData> listOfVariables = new List<GlueVariableSetData>();
            foreach (var setVariableDto in setVariableDtoList.SetVariableList)
            {
                await HandleSetVariable(setVariableDto, sendBackToGame:false, regenerateAndSave: false);

                var type = string.Join('\\', setVariableDto.InstanceOwner.Split('.').Skip(1));
                var element = ObjectFinder.Self.GetElement(type);
                var nos = element.GetNamedObjectRecursively(setVariableDto.ObjectName);

                //GlueCommands.Self.DoOnUiThread(() =>
                //    RefreshManager.Self.HandleNamedObjectValueChanged(setVariableDto.VariableName, null, nos,
                //    // record only - this variable change came from the game, we don't want to re-assign it and wipe other active edits
                //    AssignOrRecordOnly.RecordOnly)
                //);
                if(nos != null)
                {
                    var foundVariable = nos.GetCustomVariable(setVariableDto.VariableName);
                    //await VariableSendingManager.Self.HandleNamedObjectValueChanged(setVariableDto.VariableName, null, nos, AssignOrRecordOnly.RecordOnly);
                    List<GlueVariableSetData> listOfInner = VariableSendingManager.Self.GetNamedObjectValueChangedDtos(
                        setVariableDto.VariableName, null, nos, AssignOrRecordOnly.RecordOnly, gameScreenName);

                    listOfVariables.AddRange(listOfInner);

                    modifiedObjects.Add(nos);
                }
            }

            await VariableSendingManager.Self.PushVariableChangesToGame(listOfVariables);

            await TaskManager.Self.AddAsync(() =>
            {
                GlueCommands.Self.GluxCommands.SaveGlux();
                GlueCommands.Self.DoOnUiThread(GlueCommands.Self.RefreshCommands.RefreshVariables);

                HashSet<GlueElement> nosParents = new HashSet<GlueElement>();
                foreach(var nos in modifiedObjects)
                {
                    var nosParent = ObjectFinder.Self.GetElementContaining(nos);
                    if(nosParent != null)
                    {
                        nosParents.Add(nosParent);
                    }
                }

                foreach(var nosParent in nosParents)
                {
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(nosParent);
                }
            }, $"Wrapping up assignment of {setVariableDtoList.SetVariableList.Count} variables");

        }

        private static object ConvertVariable(object value, ref string typeName, string variableName, NamedObjectSave owner,
            // We have to pass this because the NOS may not have yet been added to an element if it is a new one, but we still
            // need the element to assign file names
            GlueElement nosContainer)
        {
            if(typeName == typeof(List<FlatRedBall.Math.Geometry.Point>).ToString())
            {
                value = JsonConvert.DeserializeObject<List<FlatRedBall.Math.Geometry.Point>>(value.ToString());

            } 
            else
            {
                if(typeName.StartsWith($"{GlueState.Self.ProjectNamespace}."))
                {
                    typeName = typeName.Substring(typeName.IndexOf('.') + 1);
                }
                switch (typeName)
                {
                    case "float":
                    case nameof(Single):
                        {
                            if (value is double asDouble)
                            {
                                value = (float)asDouble;
                            }
                            else if(value is int asInt)
                            {
                                value = (float)asInt;
                            }
                        }
                        break;
                    case "Microsoft.Xna.Framework.Graphics.Texture2D":
                    case nameof(Texture2D):
                    case "FlatRedBall.Graphics.Animation.AnimationChainList":
                    case nameof(AnimationChainList):
                        if (value is string asString && !string.IsNullOrEmpty(asString))
                        {
                            value =
                                FileManager.RemovePath(FileManager.RemoveExtension(asString));

                            ReferencedFileSave rfs = null;
                            if (nosContainer != null)
                            {
                                rfs = nosContainer.GetReferencedFileSaveByInstanceNameRecursively(value as string, caseSensitive:false);
                            }

                            if(rfs != null)
                            {
                                value = rfs.GetInstanceName();
                            }
                        }
                        break;
                    case nameof(TextureAddressMode):
                    case "Microsoft.Xna.Framework.Graphics.TextureAddressMode":
                        value = ToEnum<TextureAddressMode>(value);
                        break;
                    case nameof(FlatRedBall.Graphics.ColorOperation):
                    case "FlatRedBall.Graphics.ColorOperation":
                        value = ToEnum<FlatRedBall.Graphics.ColorOperation>(value);
                        break;
                    case nameof(FlatRedBall.Graphics.BlendOperation):
                    case "FlatRedBall.Graphics.BlendOperation":
                        value = ToEnum<FlatRedBall.Graphics.BlendOperation>(value);
                        break;
                    case nameof(FlatRedBall.Graphics.HorizontalAlignment):
                    case "FlatRedBall.Graphics.HorizontalAlignment":
                        value = ToEnum<FlatRedBall.Graphics.HorizontalAlignment>(value);
                        break;
                    case nameof(FlatRedBall.Graphics.VerticalAlignment):
                    case "FlatRedBall.Graphics.VerticalAlignment":
                        value = ToEnum<FlatRedBall.Graphics.VerticalAlignment>(value);
                        break;
                }
            }

            T ToEnum<T>(object toConvert)
            {
                T converted = default(T);
                if (toConvert is int asInt)
                {
                    converted = (T)(object)asInt;
                }
                else if (toConvert is long asLong)
                {
                    converted = (T)(object)(int)asLong;
                }
                return converted;
            }

            if(value is List<FlatRedBall.Math.Geometry.Point> pointList && owner.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Polygon &&
                variableName == "Points")
            {
                value = pointList.Select(item => new Vector2((float)item.X, (float)item.Y)).ToList();
                typeName = typeof(List<Vector2>).ToString();
            }

            return value;
        }

        #endregion

        #region Select Object

        private static async void HandleSelectObject(SelectObjectDto selectObjectDto)
        {

            var screen = await CommandSender.GetCurrentInGameScreen();
            TaskManager.Self.Add(() =>
            {
                NamedObjectSave nos = null;

                if(screen == null)
                {
                    var entityName = selectObjectDto.ElementNameGlue;
                    EntitySave currentEntity = ObjectFinder.Self.GetEntitySave(entityName);
                    
                    if(currentEntity != null)
                    {
                        nos = currentEntity.GetNamedObjectRecursively(selectObjectDto.NamedObject?.InstanceName);
                        if(nos == null && 
                            selectObjectDto.NamedObject?.InstanceName?.StartsWith('m') == true && selectObjectDto.NamedObject?.InstanceName?.Length > 1)
                        {
                            nos = currentEntity.GetNamedObjectRecursively(selectObjectDto.NamedObject?.InstanceName[1..]);
                        }
                    }
                }
                else
                {
                    nos = screen.GetNamedObjectRecursively(selectObjectDto.NamedObject?.InstanceName);
                }

                if(nos != null)
                {
                    GlueCommands.Self.DoOnUiThread(() =>
                    {
                        RefreshManager.Self.IgnoreNextObjectSelect = true;

                        GlueState.Self.CurrentNamedObjectSave = nos;
                        GlueCommands.Self.DialogCommands.FocusTab("Variables");
                    });
                }
            }, "Selecting object from game command");
        }

        #endregion

        #region Modify Collision

        private static void HandleDto(ModifyCollisionDto dto)
        {
            string collisionTileTypeName;
            FlatRedBall.IO.FilePath tmxFilePath;
            TiledMapSave tiledMapSave;
            MapLayer mapLayer;
            GetMapLayer(dto, out collisionTileTypeName, out tmxFilePath, out tiledMapSave, out mapLayer);

            var didChange = false;

            if (mapLayer != null)
            {

                uint tileTypeGid = 0;
                foreach (var tileset in tiledMapSave.Tilesets)
                {
                    foreach (var kvp in tileset.TileDictionary)
                    {
                        var tilesetTile = kvp.Value;
                        if (tilesetTile.Type == collisionTileTypeName)
                        {
                            tileTypeGid = kvp.Key + tileset.Firstgid;
                            break;
                        }
                    }

                    if (tileTypeGid > 0)
                    {
                        break;
                    }
                }

                var ids = mapLayer.data[0].tiles;

                // todo - support seeds
                if (dto.AddedPositions != null)
                {
                    foreach (var newTile in dto.AddedPositions)
                    {
                        int absoluteIndex = GetTileIndexFromWorldPosition(newTile, mapLayer);

                        if (absoluteIndex >= 0 && absoluteIndex < ids.Count && ids[absoluteIndex] != tileTypeGid)
                        {
                            ids[absoluteIndex] = tileTypeGid;
                            didChange = true;
                        }
                    }
                }

                if (dto.RemovedPositions != null)
                {
                    foreach (var oldTile in dto.RemovedPositions)
                    {
                        int absoluteIndex = GetTileIndexFromWorldPosition(oldTile, mapLayer);

                        if (ids[absoluteIndex] != 0)
                        {
                            ids[absoluteIndex] = 0;
                            didChange = true;
                        }
                    }
                }

                if (didChange)
                {
                    mapLayer.data[0].SetTileData(ids, mapLayer.data[0].encoding, mapLayer.data[0].compression);

                    RefreshManager.Self.IgnoreNextChange(tmxFilePath.FullPath);

                    GlueCommands.Self.TryMultipleTimes(() => tiledMapSave.Save(tmxFilePath.FullPath));

                    // Restart the screen *after* the TMX is saved, and after it has been copied too:
                    // The TMX needs to be copied which is a tasked operation:
                    if(dto.RequestRestart)
                    {
                        TaskManager.Self.Add(() =>
                        {
                            GlueCommands.Self.ProjectCommands.CopyToBuildFolder(tmxFilePath.FullPath);

                            var playBump = true;
                            // tell the game that it should restart the screen quietly
    #pragma warning disable CS4014 // Do not await in add calls this can cause problems
                            CommandSender.Send(new RestartScreenDto { ShowSelectionBump = playBump });
    #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        },
                        "Copy TMX and restart screen",
                        executionPreference: TaskExecutionPreference.Asap);
                    }

                }
            }

            static int GetTileIndexFromWorldPosition(Vector2 worldPosition, MapLayer mapLayer)
            {
                // todo - read tile size properties
                var xIndex = (int)(worldPosition.X / 16);
                var yIndex = (int)-(worldPosition.Y / 16);


                var mapWidth = mapLayer.width;
                var mapHeight = mapLayer.height;

                if (xIndex >= mapWidth || xIndex < 0 || yIndex < 0 || yIndex > mapHeight)
                {
                    return -1;
                }
                else
                {
                    var absoluteIndex = xIndex + yIndex * mapWidth;
                    return absoluteIndex;
                }

            }
        }

        private static void GetMapLayer(ModifyCollisionDto dto, out string collisionTileTypeName, out FlatRedBall.IO.FilePath tmxFilePath, out TiledMapSave tiledMapSave, out MapLayer mapLayer)
        {
            var currentScreen = GlueState.Self.CurrentScreenSave;

            var collisionNos = currentScreen.GetNamedObject(dto.TileShapeCollection);

            // this should use a file as a source, need to find that
            var sourceTmxObjectName = collisionNos.Properties.GetValue<string>("SourceTmxName");
            const int FromTypeCollisionCreationOption = 4;

            var isFromType = ObjectFinder.Self.GetPropertyValueRecursively<int>(
                collisionNos, "CollisionCreationOptions") == FromTypeCollisionCreationOption;

            collisionTileTypeName = collisionNos.Properties.GetValue<string>("CollisionTileTypeName");
            var isUsingTmx = !string.IsNullOrEmpty(sourceTmxObjectName) && isFromType && !string.IsNullOrEmpty(collisionTileTypeName);

            tmxFilePath = null;
            tiledMapSave = null;
            mapLayer = null;
            if (isUsingTmx)
            {
                var tmxObjectNos = currentScreen.GetNamedObjectRecursively(sourceTmxObjectName);
                ReferencedFileSave rfs = null;
                if (tmxObjectNos.SourceType == SourceType.File)
                {
                    rfs = currentScreen.GetReferencedFileSaveRecursively(tmxObjectNos.SourceFile);
                }

                if (rfs != null)
                {
                    tmxFilePath = GlueCommands.Self.GetAbsoluteFilePath(rfs);
                }
            }

            if (tmxFilePath?.Exists() == true)
            {
                tiledMapSave = TiledMapSave.FromFile(tmxFilePath.FullPath);
                // assume this for now...
                mapLayer = tiledMapSave.Layers.Find(item => item.Name == "GameplayLayer");
            }
        }

        #endregion
    }
}
