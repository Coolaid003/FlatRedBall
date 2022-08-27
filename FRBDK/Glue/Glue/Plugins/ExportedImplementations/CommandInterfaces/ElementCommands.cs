﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.IO.Zip;
using System.Windows.Forms;
using FlatRedBall.Glue.StandardTypes;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Errors;
using EditorObjects.SaveClasses;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Utilities;
using FlatRedBall.Glue.Projects;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Controls;
using GlueFormsCore.ViewModels;
using FlatRedBall.Glue.ViewModels;
using Microsoft.Xna.Framework;
using Glue;
using FlatRedBall.Glue.SetVariable;
using FlatRedBall.Glue.SaveClasses.Helpers;
using GlueFormsCore.Managers;
using System.Threading.Tasks;
using System.IO;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Events;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    class ElementCommands : IScreenCommands, IEntityCommands,IElementCommands
    {
        #region Fields/Properties

        static ElementCommands mSelf;
        public static ElementCommands Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new ElementCommands();
                }
                return mSelf;
            }
        }

        #endregion

        #region GlueElement (both screens and entities)

        /// <summary>
        /// Renames the argument elementToRename. The value (name) should not include
        /// the "Screens\" or "Entities\" prefix.
        /// </summary>
        /// <param name="elementToRename">The element to rename.</param>
        /// <param name="value">The desired name without the type prefix.</param>
        public async Task RenameElement(GlueElement elementToRename, string value)
        {
            await TaskManager.Self.AddAsync(() =>
            {
                bool isValid = true;
                string whyItIsntValid;
                if (elementToRename is ScreenSave)
                {
                    isValid = NameVerifier.IsScreenNameValid(value, elementToRename as ScreenSave, out whyItIsntValid);
                }
                else
                {
                    isValid = NameVerifier.IsEntityNameValid(value, elementToRename as EntitySave, out whyItIsntValid);

                }

                if (!isValid)
                {
                    GlueCommands.Self.DialogCommands.ShowMessageBox(whyItIsntValid);
                }
                else
                {

                    string oldNameFull = elementToRename.Name;
                    string newNameFull = oldNameFull.Substring(0, oldNameFull.Length - elementToRename.ClassName.Length) + value;

                    var result = ChangeClassNamesInCodeAndFileName(elementToRename, oldNameFull, newNameFull);

                    if (result == DialogResult.Yes)
                    {
                        // Set the name first because that's going
                        // to be used by code that follows to modify
                        // inheritance.
                        elementToRename.Name = newNameFull;

                        var elementsToRegenerate = new HashSet<GlueElement>();

                        if (elementToRename is EntitySave entityToRename)
                        {
                            // Change any Entities that depend on this
                            for (int i = 0; i < ProjectManager.GlueProjectSave.Entities.Count; i++)
                            {
                                var entitySave = ProjectManager.GlueProjectSave.Entities[i];
                                if (entitySave.BaseElement == oldNameFull)
                                {
                                    entitySave.BaseEntity = newNameFull;
                                }
                            }

                            // Change any NamedObjects that use this as their type (whether in Entity, or as a generic class)
                            List<NamedObjectSave> namedObjects = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(oldNameFull);

                            foreach (NamedObjectSave nos in namedObjects)
                            {
                                elementsToRegenerate.Add(ObjectFinder.Self.GetElementContaining(nos));
                                if (nos.SourceType == SourceType.Entity && nos.SourceClassType == oldNameFull)
                                {
                                    nos.SourceClassType = newNameFull;
                                    nos.UpdateCustomProperties();
                                }
                                else if (nos.SourceType == SourceType.FlatRedBallType && nos.SourceClassGenericType == oldNameFull)
                                {
                                    nos.SourceClassGenericType = newNameFull;
                                }
                                else if (nos.IsCollisionRelationship())
                                {
                                    PluginManager.CallPluginMethod(
                                        "Collision Plugin",
                                        "FixNamedObjectCollisionType",
                                        new object[] { nos });
                                }
                            }

                            // If this has a base entity, then the most base entity might be used in a list associated with factories.
                            if(!string.IsNullOrEmpty( elementToRename.BaseElement) && entityToRename.CreatedByOtherEntities)
                            {
                                var rootBase = ObjectFinder.Self.GetBaseElementRecursively(elementToRename);

                                if(rootBase != elementToRename)
                                {
                                    foreach(var nosUsingRoot in ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(rootBase as EntitySave))
                                    {
                                        elementsToRegenerate.Add(ObjectFinder.Self.GetElementContaining(nosUsingRoot));
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Change any Screens that depend on this
                            for (int i = 0; i < ProjectManager.GlueProjectSave.Screens.Count; i++)
                            {
                                var screenSave = ProjectManager.GlueProjectSave.Screens[i];
                                if (screenSave.BaseScreen == oldNameFull)
                                {
                                    screenSave.BaseScreen = newNameFull;
                                }
                            }

                            if (GlueCommands.Self.GluxCommands.StartUpScreenName == oldNameFull)
                            {
                                GlueCommands.Self.GluxCommands.StartUpScreenName = newNameFull;

                            }


                            // Don't do anything with NamedObjects and Screens since they can't (currently) be named objects

                        }

                        foreach (var element in elementsToRegenerate)
                        {
                            GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
                        }

                        GlueCommands.Self.GenerateCodeCommands.GenerateGame1();

                        GlueCommands.Self.ProjectCommands.SaveProjects();

                        GlueState.Self.CurrentGlueProject.Entities.SortByName();
                        GlueState.Self.CurrentGlueProject.Screens.SortByName();

                        GlueCommands.Self.GluxCommands.SaveGlux();


                        GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(elementToRename);

                        PluginManager.ReactToElementRenamed(elementToRename, oldNameFull);
                    }
                }
            }, $"Renaming {elementToRename} to {value}");
        }

        private DialogResult ChangeClassNamesInCodeAndFileName(GlueElement elementToRename, string oldName, string newName)
        {
            var validFiles = CodeWriter.GetAllCodeFilesFor(elementToRename);

            string oldStrippedName = FileManager.RemovePath(oldName);
            string newStrippedName = FileManager.RemovePath(newName);


            bool wasAnythingFound = false;
            List<Tuple<string, string>> oldNewAbsoluteFiles = new List<Tuple<string, string>>();

            foreach (var file in validFiles)
            {
                string newFile = file.FullPath.Replace(oldName.Replace("\\", "/"), newName.Replace("\\", "/"));

                // replace it if it's a factory:
                if (newFile.Contains("/Factories/"))
                {
                    newFile = newFile.Replace($"/Factories/{oldStrippedName}Factory.Generated.cs", $"/Factories/{newStrippedName}Factory.Generated.cs");
                }

                oldNewAbsoluteFiles.Add(new Tuple<string, string>(file.FullPath, newFile));

                if (File.Exists(newFile))
                {
                    wasAnythingFound = true;
                }

            }
            DialogResult result = DialogResult.Yes;

            if (wasAnythingFound)
            {
                result = MessageBox.Show("This rename would result in existing files being overwritten. \n\nOverwrite?", "Overwrite",
                    MessageBoxButtons.YesNo);
            }

            if (result == DialogResult.Yes)
            {
                foreach (var pair in oldNewAbsoluteFiles)
                {
                    string absoluteOldFile = pair.Item1;
                    string absoluteNewFile = pair.Item2;

                    bool isCapitalizationOnlyChange = absoluteOldFile.Equals(absoluteNewFile, StringComparison.InvariantCultureIgnoreCase);

                    if (isCapitalizationOnlyChange == false && File.Exists(absoluteNewFile))
                    {
                        FileHelper.DeleteFile(absoluteNewFile);
                    }

                    // The old files may not exist
                    // for a variety of reasons (Glue
                    // error, user manually removed the file,
                    // etc).
                    if (File.Exists(absoluteOldFile))
                    {
                        File.Move(absoluteOldFile, absoluteNewFile);
                    }

                    if (File.Exists(absoluteNewFile))
                    {
                        // Change the class name in the non-generated .cs
                        string fileContents = FileManager.FromFileText(absoluteNewFile);
                        // We call RemovePath because the name is going to be "Namespace/ClassName" and we want
                        // to find just "ClassName".
                        RefactorManager.Self.RenameClassInCode(
                            FileManager.RemovePath(oldName),
                            newStrippedName,
                            ref fileContents);

                        FileManager.SaveText(fileContents, absoluteNewFile);

                        string relativeOld = FileManager.MakeRelative(absoluteOldFile);
                        string relativeNew = FileManager.MakeRelative(absoluteNewFile);

                        ProjectManager.ProjectBase.RenameItem(relativeOld, relativeNew);

                        foreach (VisualStudioProject syncedProject in GlueState.Self.SyncedProjects)
                        {
                            string syncedRelativeOld = FileManager.MakeRelative(absoluteOldFile, syncedProject.Directory);
                            string syncedRelativeNew = FileManager.MakeRelative(absoluteNewFile, syncedProject.Directory);
                            syncedProject.RenameItem(syncedRelativeOld, syncedRelativeNew);
                        }
                    }
                }
            }
            return result;
        }


        #endregion

        #region Add Screen

        public async Task<SaveClasses.ScreenSave> AddScreen(string screenName)
        {
            ScreenSave screenSave = new ScreenSave();
            screenSave.Name = @"Screens\" + screenName;

            await AddScreen(screenSave, suppressAlreadyExistingFileMessage:false);

            return screenSave;
        }

        public async Task AddScreen(ScreenSave screenSave, bool suppressAlreadyExistingFileMessage = false)
        {
            var glueProject = GlueState.Self.CurrentGlueProject;

            string screenName = FileManager.RemovePath(screenSave.Name);

            string fileName = screenSave.Name + ".cs";

            screenSave.Tags.Add("GLUE");
            screenSave.Source = "GLUE";

            glueProject.Screens.Add(screenSave);
            glueProject.Screens.SortByName();

            #region Create the Screen code (not the generated version)


            var fullNonGeneratedFileName = FileManager.RelativeDirectory + fileName;
            var addedScreen = 
                GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(fullNonGeneratedFileName, save:false);


            string projectNamespace = ProjectManager.ProjectNamespace;

            StringBuilder stringBuilder = new StringBuilder(CodeWriter.ScreenTemplateCode);

            CodeWriter.SetClassNameAndNamespace(
                projectNamespace + ".Screens",
                screenName,
                stringBuilder);

            string modifiedTemplate = stringBuilder.ToString();


            if (addedScreen == null)
            {
                if (!suppressAlreadyExistingFileMessage)
                {
                    MessageBox.Show("There is already a file named\n\n" + fullNonGeneratedFileName + "\n\nThis file will be used instead of creating a new one just in case you have code that you want to keep there.");
                }
            }
            else
            {

                FileManager.SaveText(
                    modifiedTemplate,
                    fullNonGeneratedFileName
                    );
            }


            #endregion

            #region Create <ScreenName>.Generated.cs

            string generatedFileName = @"Screens\" + screenName + ".Generated.cs";
            ProjectManager.CodeProjectHelper.CreateAndAddPartialCodeFile(generatedFileName, true);


            #endregion

            // We used to set the 
            // StartUpScreen whenever
            // the user made a new Screen.
            // The reason is we assumed that
            // the user wanted to work on this
            // Screen, so we set it as the startup
            // so they could run the game right away.
            // Now we only want to do it if there are no
            // other Screens.  Otherwise they can just use
            // GlueView.
            if (glueProject.Screens.Count == 1)
            {
                GlueState.Self.CurrentGlueProject.StartUpScreen = screenSave.Name;
                GlueCommands.Self.GenerateCodeCommands.GenerateStartupScreenCode();
            }
            GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(screenSave);

            GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(screenSave);

            await PluginManager.ReactToNewScreenCreated(screenSave);


            GlueCommands.Self.ProjectCommands.SaveProjects();

            GluxCommands.Self.SaveGlux();
        }

        #endregion

        #region Add Entity

        public SaveClasses.EntitySave AddEntity(string entityName, bool is2D = false)
        {

            string fileName = entityName + ".cs";

            if (!entityName.ToLower().StartsWith("entities\\") && !entityName.ToLower().StartsWith("entities/"))
            {
                fileName = @"Entities\" + fileName;
            }



            EntitySave entitySave = new EntitySave();
            entitySave.Is2D = is2D;
            entitySave.Name = FileManager.RemoveExtension(fileName);

            const bool AddXYZ = true;

            if (AddXYZ)
            {
                entitySave.CustomVariables.Add(new CustomVariable() { Name = "X", Type = "float" });
                entitySave.CustomVariables.Add(new CustomVariable() { Name = "Y", Type = "float" });
                entitySave.CustomVariables.Add(new CustomVariable() { Name = "Z", Type = "float" });
            }

            AddEntity(entitySave);

            return entitySave;

        }

        public async Task<SaveClasses.EntitySave> AddEntityAsync(AddEntityViewModel viewModel, string directory = null)
        {
            var gluxCommands = GlueCommands.Self.GluxCommands;

            var newElement = gluxCommands.EntityCommands.AddEntity(
                directory + viewModel.Name, is2D: true);

            GlueState.Self.CurrentElement = newElement;

            var hasInheritance = false;
            if(viewModel.HasInheritance)
            {
                newElement.BaseEntity = viewModel.SelectedBaseEntity;

                EditorObjects.IoC.Container.Get<SetPropertyManager>().ReactToPropertyChanged(
                    nameof(newElement.BaseEntity), false, nameof(newElement.BaseEntity), null);

                hasInheritance = true;
            }

            if (viewModel.IsSpriteChecked)
            {
                AddObjectViewModel addObjectViewModel = new AddObjectViewModel();
                addObjectViewModel.ObjectName = "SpriteInstance";
                addObjectViewModel.SelectedAti = AvailableAssetTypes.CommonAtis.Sprite;
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                await gluxCommands.AddNewNamedObjectToSelectedElementAsync(addObjectViewModel);
                GlueState.Self.CurrentElement = newElement;
            }

            if (viewModel.IsTextChecked)
            {
                AddObjectViewModel addObjectViewModel = new AddObjectViewModel();
                addObjectViewModel.ObjectName = "TextInstance";
                addObjectViewModel.SelectedAti = AvailableAssetTypes.CommonAtis.Text;
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                await gluxCommands.AddNewNamedObjectToSelectedElementAsync(addObjectViewModel);
                GlueState.Self.CurrentElement = newElement;
            }

            if (viewModel.IsCircleChecked)
            {
                AddObjectViewModel addObjectViewModel = new AddObjectViewModel();
                addObjectViewModel.ObjectName = "CircleInstance";
                addObjectViewModel.SelectedAti = AvailableAssetTypes.CommonAtis.Circle;
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                await gluxCommands.AddNewNamedObjectToSelectedElementAsync(addObjectViewModel);
                GlueState.Self.CurrentElement = newElement;
            }

            if (viewModel.IsAxisAlignedRectangleChecked)
            {
                AddObjectViewModel addObjectViewModel = new AddObjectViewModel();
                addObjectViewModel.ObjectName = "AxisAlignedRectangleInstance";
                addObjectViewModel.SelectedAti = AvailableAssetTypes.CommonAtis.AxisAlignedRectangle;
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                await gluxCommands.AddNewNamedObjectToSelectedElementAsync(addObjectViewModel);
                GlueState.Self.CurrentElement = newElement;
            }

            // There are a few important things to note about this function:
            // 1. Whenever gluxCommands.AddNewNamedObjectToSelectedElement is called, Glue performs a full
            //    refresh and save. The reason for this is that gluxCommands.AddNewNamedObjectToSelectedElement
            //    is the standard way to add a new named object to an element, and it may be called by other parts
            //    of the code (and plugins) that expect the add to be a complete set of logic (add, refresh, save, etc).
            //    This is less efficient than adding all of them and saving only once, but that would require a second add
            //    method, which would add complexity. For now, we deal with the slower calls because it's not really noticeable.
            // 2. Some actions, like adding Points to a polygon, are done after the polygon is created and added, and that requires
            //    an additional save. Therefore, we do one last save/refresh at the end of this method in certain situations.
            //    Again, this is less efficient than if we performed just a single call, but a single call would be more complicated.
            //    because we'd have to suppress all the other calls.
            bool needsRefreshAndSave = false;

            if (viewModel.IsPolygonChecked)
            {
                AddObjectViewModel addObjectViewModel = new AddObjectViewModel();
                addObjectViewModel.ObjectName = "PolygonInstance";
                addObjectViewModel.SelectedAti = AvailableAssetTypes.CommonAtis.Polygon;
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;

                var nos = await gluxCommands.AddNewNamedObjectToSelectedElementAsync(addObjectViewModel);
                CustomVariableInNamedObject instructions = null;
                instructions = nos.GetCustomVariable("Points");
                if (instructions == null)
                {
                    instructions = new CustomVariableInNamedObject();
                    instructions.Member = "Points";
                    nos.InstructionSaves.Add(instructions);
                }
                var points = new List<Vector2>();
                points.Add(new Vector2(-16, 16));
                points.Add(new Vector2(16, 16));
                points.Add(new Vector2(16, -16));
                points.Add(new Vector2(-16, -16));
                points.Add(new Vector2(-16, 16));
                instructions.Value = points;


                needsRefreshAndSave = true;

                GlueState.Self.CurrentElement = newElement;
            }

            if(!hasInheritance)
            {
                if (viewModel.IsIVisibleChecked)
                {
                    newElement.ImplementsIVisible = true;
                    needsRefreshAndSave = true;
                }

                if (viewModel.IsIClickableChecked)
                {
                    newElement.ImplementsIClickable = true;
                    needsRefreshAndSave = true;
                }

                if (viewModel.IsIWindowChecked)
                {
                    newElement.ImplementsIWindow = true;
                    needsRefreshAndSave = true;
                }

                if (viewModel.IsICollidableChecked)
                {
                    newElement.ImplementsICollidable = true;
                    needsRefreshAndSave = true;
                }
            }

            // even derived entities can have factories
            if(viewModel.IsCreateFactoryChecked)
            {
                newElement.CreatedByOtherEntities = true;
                needsRefreshAndSave = true;
            }

            PluginManager.ReactToNewEntityCreated(newElement);
            if (needsRefreshAndSave)
            {
                GlueCommands.Self.DoOnUiThread(() =>
                {
                    MainGlueWindow.Self.PropertyGrid.Refresh();
                });
                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(newElement);
                GluxCommands.Self.SaveGlux();
            }

            return newElement;
        }


        public void AddEntity(EntitySave entitySave)
        {
            AddEntity(entitySave, false);
        }

        public void AddEntity(EntitySave entitySave, bool suppressAlreadyExistingFileMessage)
        {

            entitySave.Tags.Add("GLUE");
            entitySave.Source = "GLUE";

            var glueProject = GlueState.Self.CurrentGlueProject;

            glueProject.Entities.Add(entitySave);

            glueProject.Entities.SortByName();

            var customCodeFilePath =
                GlueCommands.Self.FileCommands.GetCustomCodeFilePath(entitySave);
            #region Create the Entity custom code file (not the generated version)



            var newItem = GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(
                customCodeFilePath, false);

            string projectNamespace = GlueState.Self.ProjectNamespace;


            string directory = FileManager.GetDirectory(entitySave.Name);
            if (!directory.ToLower().EndsWith(projectNamespace.ToLower() + "/entities/"))
            {
                GlueCommands.Self.GenerateCodeCommands.GetNamespaceForElement(entitySave);
                // test this on doubly-embedded Entities.
                projectNamespace = GlueCommands.Self.GenerateCodeCommands.GetNamespaceForElement(entitySave);
                // += (".Entities." + FileManager.RemovePath(directory)).Replace("/", "");
            }

            StringBuilder stringBuilder = new StringBuilder(CodeWriter.EntityTemplateCode);

            CodeWriter.SetClassNameAndNamespace(
                projectNamespace,
                entitySave.ClassName,
                stringBuilder);

            string modifiedTemplate = stringBuilder.ToString();


            if (newItem == null)
            {
                if (!suppressAlreadyExistingFileMessage)
                {
                    MessageBox.Show("There is already a file named\n\n" + customCodeFilePath + "\n\nThis file will be used instead of creating a new one just in case you have code that you want to keep there.");
                }
            }
            else
            {
                FileManager.SaveText(modifiedTemplate, customCodeFilePath.FullPath);
            }
            #endregion

            #region Create <EntityName>.Generated.cs

            string generatedFileName = FileManager.MakeRelative(directory).Replace("/", "\\") + entitySave.ClassName + ".Generated.cs";

            ProjectManager.CodeProjectHelper.CreateAndAddPartialCodeFile(generatedFileName, true);

            #endregion

            GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(entitySave);

            GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(entitySave);

            GlueCommands.Self.ProjectCommands.SaveProjects();

            GluxCommands.Self.SaveGlux();
        }

        #endregion

        #region Add CustomVariable

        public void AddCustomVariableToCurrentElement(CustomVariable newVariable, bool save = true)
        {
            var element = GlueState.Self.CurrentElement;
            AddCustomVariableToElement(newVariable, element, save);
        }

        public async Task AddStateCategoryCustomVariableToElementAsync(StateSaveCategory category, GlueElement element, bool save = true)
        {
            await TaskManager.Self.AddAsync(() =>
            {
                // expose a variable that exposes the category
                CustomVariable customVariable = new CustomVariable();

                customVariable.Type = category.Name;
                customVariable.Name = "Current" + category.Name + "State";

                GlueCommands.Self.GluxCommands.ElementCommands.AddCustomVariableToElement(
                    customVariable, element, save);

            }, $"Adding category {category} as variable to {element}");
        }

        public void AddCustomVariableToElement(CustomVariable newVariable, GlueElement element, bool save = true)
        { 
            element.CustomVariables.Add(newVariable);

            // by default new variables should not be included in states. 
            foreach(var category in element.StateCategoryList)
            {
                if (!category.ExcludedVariables.Contains(newVariable.Name))
                { 
                    category.ExcludedVariables.Add(newVariable.Name);
                }
            }

            InheritanceManager.UpdateAllDerivedElementFromBaseValues(true, element);


            CustomVariableHelper.SetDefaultValueFor(newVariable, element);

            GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(element);

            GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();

            UpdateInstanceCustomVariables(element);

            PluginManager.ReactToVariableAdded(newVariable);

            // Generate code after PluginMangager.React so that the code can include any changes made by plugins.
            GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);

            if (GlueState.Self.CurrentElement == element)
            {
                // Vic asks = why do we call ReactToItemSelect instead of setting the custom variable. Is it to force a refresh?
                // On because actually people usually don't want to select the variable because it's rare to actually modify the variable
                // through its properties. Instead, it's more common to select the variables folder and use the variables tab
                //GlueState.Self.CurrentCustomVariable = newVariable;
                PluginManager.ReactToItemSelect(GlueState.Self.CurrentTreeNode);
            }

            if (save)
            {
                GluxCommands.Self.SaveGlux();
            }

        }

        public async Task AddCustomVariableToElementAsync(CustomVariable newVariable, GlueElement element, bool save = true)
        {
            await TaskManager.Self.AddAsync(() => AddCustomVariableToElement(newVariable, element, save),
                $"Adding variable {newVariable.Name} to {element}");
        }


        private void UpdateInstanceCustomVariables(IElement currentElement)
        {
            List<NamedObjectSave> namedObjectsToUpdate = null;

            if (currentElement is EntitySave)
            {
                namedObjectsToUpdate = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(currentElement.Name);
            }

            if (namedObjectsToUpdate != null)
            {
                foreach (NamedObjectSave nos in namedObjectsToUpdate)
                {
                    nos.UpdateCustomProperties();
                }
            }
        }
        #endregion

        #region Add StateSaveCategory

        public async Task AddStateSaveCategoryAsync(string categoryName, GlueElement element)
        {
            await TaskManager.Self.AddAsync(() =>
            {
                var newCategory = new StateSaveCategory();
                newCategory.Name = categoryName;

                foreach (var variable in element.CustomVariables)
                {
                    // new categories should have all variables excluded initially.
                    newCategory.ExcludedVariables.Add(variable.Name);
                }

                element.StateCategoryList.Add(newCategory);

                List<NamedObjectSave> nosList = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(element.Name);
                List<EntitySave> derivedEntities = ObjectFinder.Self.GetAllEntitiesThatInheritFrom(element.Name);
                for (int i = 0; i < derivedEntities.Count; i++)
                {
                    EntitySave entitySave = derivedEntities[i];

                    nosList.AddRange(ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(entitySave.Name));
                }

                foreach (NamedObjectSave nos in nosList)
                {
                    nos.UpdateCustomProperties();
                }

                GlueCommands.Self.RefreshCommands.RefreshCurrentElementTreeNode();
                GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();

                GlueState.Self.CurrentStateSaveCategory = newCategory;

                GluxCommands.Self.SaveGlux();
            }, nameof(AddStateSaveCategoryAsync));
        }

        #endregion

        #region ReferencedFile

        [Obsolete("This function does way too much. Moving this to GluxCommands")]
        public ReferencedFileSave CreateReferencedFileSaveForExistingFile(IElement containerForFile, string directoryInsideContainer, string absoluteFileName,
            PromptHandleEnum unknownTypeHandle, AssetTypeInfo ati, out string creationReport, out string errorMessage)
        {
            creationReport = "";
            errorMessage = null;

            ReferencedFileSave referencedFileSaveToReturn = null;

            string whyItIsntValid;
            // Let's see if there is already an Entity with the same name
            string fileWithoutPath = FileManager.RemovePath(FileManager.RemoveExtension(absoluteFileName));

            bool isValid = 
                NameVerifier.IsReferencedFileNameValid(fileWithoutPath, ati, referencedFileSaveToReturn, containerForFile, out whyItIsntValid);

            if (!isValid)
            {
                errorMessage = "Invalid file name:\n" + fileWithoutPath + "\n" + whyItIsntValid;
            }
            else
            {
                Zipper.UnzipAndModifyFileIfZip(ref absoluteFileName);
                string extension = FileManager.GetExtension(absoluteFileName);
                    
                bool isValidExtensionOrIsConfirmedByUser;
                bool isUnknownType;
                CheckAndWarnAboutUnknownFileTypes(unknownTypeHandle, extension, out isValidExtensionOrIsConfirmedByUser, out isUnknownType);

                string fileToAdd = null;
                if (isValidExtensionOrIsConfirmedByUser)
                {

                    string directoryThatFileShouldBeRelativeTo = GetFullPathContentDirectory(containerForFile, directoryInsideContainer);

                    string projectDirectory = ProjectManager.ContentProject.GetAbsoluteContentFolder();

                    bool needsToCopy = !FileManager.IsRelativeTo(absoluteFileName, projectDirectory);


                    if (needsToCopy)
                    {
                        fileToAdd = directoryThatFileShouldBeRelativeTo + FileManager.RemovePath(absoluteFileName);
                        fileToAdd = FileManager.MakeRelative(fileToAdd, ProjectManager.ContentProject.GetAbsoluteContentFolder());

                        try
                        {
                            FileHelper.RecursivelyCopyContentTo(absoluteFileName,
                                FileManager.GetDirectory(absoluteFileName),
                                directoryThatFileShouldBeRelativeTo);
                        }
                        catch (System.IO.FileNotFoundException fnfe)
                        {
                            errorMessage = "Could not copy the files because of a missing file: " + fnfe.Message;
                        }
                    }
                    else
                    {
                        fileToAdd = GetNameOfFileRelativeToContentFolder(absoluteFileName, directoryThatFileShouldBeRelativeTo, projectDirectory);

                    }

                }

                if(string.IsNullOrEmpty(errorMessage))
                { 
                    BuildToolAssociation bta = null;

                    if (ati != null && !string.IsNullOrEmpty(ati.CustomBuildToolName))
                    {
                        bta =
                            BuildToolAssociationManager.Self.GetBuilderToolAssociationByName(ati.CustomBuildToolName);
                    }

                    if (containerForFile != null)
                    {
                        referencedFileSaveToReturn = containerForFile.AddReferencedFile(fileToAdd, ati, bta);
                    }
                    else
                    {
                        bool useFullPathAsName = false;
                        // todo - support built files here
                        referencedFileSaveToReturn =
                            GlueCommands.Self.GluxCommands.AddReferencedFileToGlobalContent(fileToAdd, useFullPathAsName);
                    }



                    // This will be null if there was an error above in creating this file
                    if (referencedFileSaveToReturn != null)
                    {
                        if (containerForFile != null)
                            containerForFile.HasChanged = true;

                        if (fileToAdd.EndsWith(".csv"))
                        {
                            string fileToAddAbsolute = ProjectManager.MakeAbsolute(fileToAdd);
                            CsvCodeGenerator.GenerateAndSaveDataClass(referencedFileSaveToReturn, referencedFileSaveToReturn.CsvDelimiter);
                        }
                        if (isUnknownType)
                        {
                            referencedFileSaveToReturn.LoadedAtRuntime = false;
                        }

                        GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(referencedFileSaveToReturn);

                        PluginManager.ReactToNewFile(referencedFileSaveToReturn);
                        GluxCommands.Self.SaveGlux();
                        GlueCommands.Self.ProjectCommands.SaveProjects();
                        UnreferencedFilesManager.Self.RefreshUnreferencedFiles(false);

                        string error;
                        referencedFileSaveToReturn.RefreshSourceFileCache(false, out error);

                        if (!string.IsNullOrEmpty(error))
                        {
                            ErrorReporter.ReportError(referencedFileSaveToReturn.Name, error, false);
                        }
                    }
                }
            }

            return referencedFileSaveToReturn;
        }

        public static string GetNameOfFileRelativeToContentFolder(string absoluteSourceFileName, string directoryThatFileShouldBeRelativeTo, string projectDirectory)
        {
            string fileToAdd = "";
            var rfs = GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(absoluteSourceFileName);

            if (rfs != null)
            {
                fileToAdd = rfs.Name;
            }
            else
            {
                fileToAdd = FileManager.MakeRelative(absoluteSourceFileName, ProjectManager.ContentProject.GetAbsoluteContentFolder());
            }
            return fileToAdd;
        }

        public static void CheckAndWarnAboutUnknownFileTypes(PromptHandleEnum unknownTypeHandle, string extension, out bool isValidExtensionOrIsConfirmedByUser, out bool isUnknownType)
        {
            isValidExtensionOrIsConfirmedByUser = true;
            isUnknownType = false;

            if (AvailableAssetTypes.Self.GetAssetTypeFromExtension(extension) == null && extension != "csv")
            {
                DialogResult dialogResult = DialogResult.Yes;
                bool addToList;

                if (!AvailableAssetTypes.Self.AdditionalExtensionsToTreatAsAssets.Contains(extension))
                {
                    switch (unknownTypeHandle)
                    {
                        case PromptHandleEnum.Prompt:
                            dialogResult = MessageBox.Show("The extension " + extension + " is not recognized by Glue.  " +
                                                           "Glue will not be able to generate code for this file, but will add it to your game project.\n\nDo you " +
                                                           "want to add this file?", "Add unknown type?", MessageBoxButtons.YesNo);
                            break;
                        case PromptHandleEnum.DoYes:
                            dialogResult = DialogResult.Yes;
                            break;
                        case PromptHandleEnum.DoNo:
                            dialogResult = DialogResult.No;
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    addToList = true;
                }
                else
                {
                    // This means the user has already said "yes" to adding this type
                    dialogResult = DialogResult.Yes;
                    addToList = false;
                }


                if (dialogResult == DialogResult.No)
                {
                    isValidExtensionOrIsConfirmedByUser = false;
                }
                else
                {
                    isUnknownType = true;
                    if (addToList)
                    {
                        AvailableAssetTypes.Self.AdditionalExtensionsToTreatAsAssets.Add(extension);
                    }
                }
            }
        }

        public static string GetFullPathContentDirectory(IElement element, string directoryRelativeToElement)
        {
            string resultNameInFolder = "";

            if (!String.IsNullOrEmpty(directoryRelativeToElement))
            {
                //string directory = directoryTreeNode.GetRelativePath().Replace("/", "\\");

                resultNameInFolder = directoryRelativeToElement;
            }
            else if (element != null)
            {
                //string directory = elementToAddTo.GetRelativePath().Replace("/", "\\");

                resultNameInFolder = element.Name.Replace(@"/", @"\");
            }
            else
            {
                resultNameInFolder = "GlobalContent/";
            }

            if (!resultNameInFolder.EndsWith("\\") && !resultNameInFolder.EndsWith("/"))
            {
                resultNameInFolder += "\\";
            }


            return ProjectManager.ContentDirectory + resultNameInFolder;
        }

        #endregion

        #region Events

        public async Task AddEventToElement(AddEventViewModel viewModel, GlueElement glueElement)
        {

            string eventName = viewModel.EventName;

            string failureMessage;
            bool isInvalid = NameVerifier.IsEventNameValid(eventName,
                glueElement, out failureMessage);

            if (isInvalid)
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox(failureMessage);
            }
            else if (!isInvalid)
            {
                await TaskManager.Self.AddAsync(() =>
                {
                    EventResponseSave eventResponseSave = new EventResponseSave();
                    eventResponseSave.EventName = eventName;

                    eventResponseSave.SourceObject = viewModel.TunnelingObject;
                    eventResponseSave.SourceObjectEvent = viewModel.TunnelingEvent;

                    eventResponseSave.SourceVariable = viewModel.SourceVariable;
                    eventResponseSave.BeforeOrAfter = viewModel.BeforeOrAfter;

                    eventResponseSave.DelegateType = viewModel.DelegateType;

                    AddEventToElement(glueElement, eventResponseSave);
                }, $"Adding element {viewModel.EventName}");
            }
        }

        public void AddEventToElement(GlueElement currentElement, EventResponseSave eventResponseSave)
        {
            currentElement.Events.Add(eventResponseSave);

            string fullGeneratedFileName = ProjectManager.ProjectBase.Directory + EventManager.GetGeneratedEventFileNameForElement(currentElement);

            if (!File.Exists(fullGeneratedFileName))
            {
                CodeWriter.AddEventGeneratedCodeFileForElement(currentElement);
            }

            GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();

            GlueCommands.Self.RefreshCommands.RefreshCurrentElementTreeNode();

            GluxCommands.Self.SaveGlux();

            GlueState.Self.CurrentEventResponseSave = eventResponseSave;
        }
        #endregion

        /// <summary>
        /// Updates the argument glueElement from its base types. This updates variables and named objects.
        /// </summary>
        /// <param name="glueElement">The base Glue element to update.</param>
        /// <returns>Whether the object updated</returns>
        public bool UpdateFromBaseType(GlueElement glueElement)
        {
            bool haveChangesOccurred = false;
            if (ObjectFinder.Self.GlueProject != null)
            {
                haveChangesOccurred |= ElementExtensionMethods.UpdateNamedObjectsFromBaseType(glueElement);

                ElementExtensionMethods.UpdateCustomVariablesFromBaseType(glueElement);
            }
            return haveChangesOccurred;
        }


    }
}
