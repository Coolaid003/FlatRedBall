﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Elements;
using System.Diagnostics;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.Utilities;
using EditorObjects.Parsing;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.Events;
using EditorObjects.Cleaners;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Instructions;
using FlatRedBall.Glue.ContentPipeline;
using FlatRedBall.Glue.Projects;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Navigation;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.TypeConversions;
using System.Drawing;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using System.ServiceModel;
//using GlueWcfServices;
//using Glue.Wcf;
//using FlatRedBall.Glue.Wcf;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Data;
//using System.Management;
using FlatRedBall.Glue.SetVariable;
using Container = EditorObjects.IoC;
using FlatRedBall.Glue.UnreferencedFiles;
using FlatRedBall.Glue.Controls.ProjectSync;
using System.Linq;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using System.Threading.Tasks;
using FlatRedBall.Instructions.Reflection;
using Microsoft.Xna.Framework.Audio;
using GlueFormsCore.Plugins.EmbeddedPlugins.ExplorerTabPlugin;
using System.Windows.Forms.Integration;
using AvalonDock;
using AvalonDock.Layout;
//using System.Windows;

//using EnvDTE;

namespace Glue
{
    public partial class MainGlueWindow : Form
    {
        public bool HasErrorOccurred = false;
        private System.Windows.Forms.Timer FileWatchTimer;

        private static MainGlueWindow mSelf;

        public static MainGlueWindow Self
        {
            get { return mSelf; }
        }

        MainExplorerPlugin mainExplorerPlugin;
        private GlueFormsCore.Controls.WinformsSplitContainer MainPanelSplitContainer;



        public System.ComponentModel.IContainer Components => components;

        public System.Windows.Forms.PropertyGrid PropertyGrid;

        private int NumberOfStoredRecentFiles
        {
            get;
            set;
        }

        DockingManager _dockingManager = new DockingManager();

        //LayoutDocumentPaneGroup centerDocumentPane;
        LayoutDocumentPane centerSide;
        LayoutDocumentPane leftSide;
        LayoutDocumentPane rightSide;
        //LayoutDocumentPane bottomSide;

        //LayoutAnchorGroup leftSide;
        ////LayoutAnchorablePane leftSide;
        //LayoutAnchorGroup rightSide;
        LayoutAnchorGroup bottomSide;
        LayoutAnchorGroup topSide;

        public MainGlueWindow()
        {
            mSelf = this;

            InitializeComponent();

            this.MainPanelSplitContainer = new GlueFormsCore.Controls.WinformsSplitContainer();
            //this.Controls.Add(this.MainPanelSplitContainer);
            System.Windows.Forms.Integration.ElementHost wpfHost;
            wpfHost = new System.Windows.Forms.Integration.ElementHost();
            wpfHost.Dock = DockStyle.Fill;
            wpfHost.Location = new Point(0, 54);

            //LayoutDocument doc = new LayoutDocument() { Title = "test" };
            //_dockingManager.Layout.Descendents().OfType<LayoutDocumentPane>().First().Children.Add(doc);

            //dockingManagerHost.Child = _dockingManager;
            //leftSide = new LayoutAnchorGroup();
            //rightSide = new LayoutAnchorGroup();
            ////centerSide = new LayoutAnchorablePane();
            //bottomSide = new LayoutAnchorGroup();

            LayoutDocumentPane AddPane(System.Windows.Controls.Orientation orientation = System.Windows.Controls.Orientation.Horizontal)
            {
                var pane = new LayoutDocumentPane();

                var group = new LayoutDocumentPaneGroup();
                _dockingManager.Layout.RootPanel.Children.Add(group);

                group.Children.Add(pane);
                group.Orientation = orientation;
                return pane;
            }

            _dockingManager.Layout.RootPanel.Children.Clear();

            //bottomSide = AddPane(System.Windows.Controls.Orientation.Vertical);
            leftSide = AddPane();
            centerSide = AddPane();
            rightSide = AddPane();
            bottomSide = new LayoutAnchorGroup();
            topSide = new LayoutAnchorGroup();



            //_dockingManager.Layout.LeftSide.Children.Add(leftSide);
            //_dockingManager.Layout.RightSide.Children.Add(rightSide);
            _dockingManager.Layout.BottomSide.Children.Add(bottomSide);
            _dockingManager.Layout.TopSide.Children.Add(topSide);

            //var layoutPanel = new LayoutPanel();
            //layoutPanel.Children.Add(centerSide);
            //_dockingManager.Layout.Children..RootPanel.Children.Add(layoutPanel);
            wpfHost.Child = _dockingManager;

            //for (int i = 0; i < 3; i++)
            //{
            //    var element = new System.Windows.Controls.Button();
            //    element.Width = 100;
            //    element.Height = 200;
            //    element.Content = "Help" + i;
            //    AddControl(element, "Help Titlle" + i, TabLocation.Center);

            //}


            var element = new System.Windows.Controls.Button();
            AddControl(element, "Bottomo", TabLocation.Bottom);

            element = new System.Windows.Controls.Button();
            AddControl(element, "Help Titlle", TabLocation.Right);

            element = new System.Windows.Controls.Button();
            AddControl(element, "Help Titlle", TabLocation.Left);

            element = new System.Windows.Controls.Button();
            AddControl(element, "Help Titlle", TabLocation.Top);



            //var rootPanel = _dockingManager.Layout.RootPanel;


            //_dockingManager.Theme = new Vs2013LightTheme();

            this.Controls.Add(wpfHost);
            this.Controls.SetChildIndex(wpfHost, 0);

            //bottomSide.Children.First().ToggleAutoHide();

            this.FileWatchTimer = new System.Windows.Forms.Timer(this.components);

            this.FileWatchTimer.Enabled = true;
            // the frequency of file change flushes. Reducing this time
            // makes Glue more responsive, but increases the chance of 
            // Glue performing a check mid update like on a git pull.
            // Note that the ChangeInformation also keeps a timer since the last
            // file was added, and will wait mMinimumTimeAfterChangeToReact until 
            // flushing.
            this.FileWatchTimer.Interval = 400;
            this.FileWatchTimer.Tick += new System.EventHandler(this.FileWatchTimer_Tick);

        }

        public LayoutContent AddControl(System.Windows.FrameworkElement element, string title, TabLocation tabLocation)
        {
            //anchorable.AutoHideWidth = 240;
            //centerSide.Children.Add(anchorable);

            //.Children.Add(anchorable);
            //anchorable.CanAutoHide = false;

            LayoutDocument CreateLayoutDocument()
            {
                var layout = new LayoutDocument();
                layout.IsMaximized = true;
                layout.Content = element;

                layout.Title = title;

                return layout;
            }

            LayoutAnchorable CreateLayoutAnchorable()
            {
                var layout = new LayoutAnchorable();
                layout.Content = element;
                layout.Title = title;
                //layout.CanAutoHide = false;
                //layout.IsMaximized = true;
                //layout.FloatingHeight = 200;
                //pane.Children.Add(layout);

                return layout;
            }

            LayoutContent toReturn = null;
            if (tabLocation == TabLocation.Left)
            {
                leftSide.Children.Add(CreateLayoutDocument());
                toReturn = leftSide.Children.Last();
            }
            else if (tabLocation == TabLocation.Center)
            {
                centerSide.Children.Add(CreateLayoutDocument());
                toReturn = centerSide.Children.Last();
            }
            else if (tabLocation == TabLocation.Right)
            {
                rightSide.Children.Add(CreateLayoutDocument());
                toReturn = rightSide.Children.Last();

            }
            else if( tabLocation == TabLocation.Bottom)
            {
                //var pane = new LayoutAnchorablePane();
                //bottomSide.Children.Add(pane);

                bottomSide.Children.Add(CreateLayoutAnchorable());
                toReturn = bottomSide.Children.Last();

            }
            else if (tabLocation == TabLocation.Top)
            {
                topSide.Children.Add(CreateLayoutAnchorable());
                toReturn = topSide.Children.Last();

            }
            return toReturn;
        }


        public LayoutContent AddControl(System.Windows.Forms.Control winformsControl, string title, TabLocation tabLocation)
        {
            WindowsFormsHost host = new WindowsFormsHost();
            host.Child = winformsControl;
            return AddControl(host, title, tabLocation);
        }

        public void Invoke(Action action)
        {
            this.Invoke((MethodInvoker)delegate
            {
                try
                {
                    action();
                }
                catch(Exception e)
                {
                    if(!IsDisposed)
                    {
                        throw e;
                    }
                    // otherwise, we don't care, they're exiting
                }
            });

        }

        public async Task LoadProject(string projectFileName, InitializationWindow initializationWindow)
        {
            await ProjectLoader.Self.LoadProject(projectFileName, initializationWindow);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                StartUpGlue();
            }
            catch (FileNotFoundException fnfe)
            {
                if (fnfe.ToString().Contains("Microsoft.Xna.Framework.dll"))
                {
                    var message = "Could not load Glue, probably because XNA 4 is not installed. Click OK to go to the XNA runtime page to install the XNA runtime, then run Glue again";
                    MessageBox.Show(message);
                    System.Diagnostics.Process.Start("https://www.microsoft.com/en-us/download/details.aspx?id=20914");
                    this.Close();
                }
                else
                {
                    throw fnfe;
                }
            }
        }

        internal void StartUpGlue()
        {
            // Some stuff can be parallelized.  We're going to run stuff
            // that can be parallelized in parallel, and then block to wait for
            // all tasks to finish when we need to

            AddObjectsToIocContainer();

            AddErrorReporters();

            InitializationWindow initializationWindow = new InitializationWindow();

            // Initialize GlueGui before using it:
            GlueGui.Initialize(mMenu);
            GlueGui.ShowWindow(initializationWindow, this);

            initializationWindow.Message = "Initializing Glue Systems";
            Application.DoEvents();

            // Add Glue.Common
            PropertyValuePair.AdditionalAssemblies.Add(typeof(PlatformSpecificType).Assembly);

            // Monogame:
            PropertyValuePair.AdditionalAssemblies.Add(typeof(SoundEffectInstance).Assembly);

            // Async stuff
            {

                initializationWindow.SubMessage = "Initializing WCF"; Application.DoEvents();
                //TaskManager.Self.AddAsyncTask(() => WcfManager.Self.Initialize(), "Initializing WCF");

                initializationWindow.SubMessage = "Initializing EventManager"; Application.DoEvents();
                TaskManager.Self.Add(() => EventManager.Initialize(), "Initializing EventManager");

                Application.DoEvents();

                initializationWindow.SubMessage = "Initializing ExposedVariableManager"; Application.DoEvents();
                try
                {
                    ExposedVariableManager.Initialize();
                }
                catch (Exception excep)
                {
                    TaskManager.Self.AddAsyncTask(() => 
                        GlueGui.ShowException("Could not load assemblies - you probably need to rebuild Glue.", "Error", excep),
                        "Show error message about not being able to load assemblies");

                    return;
                }
            }

            initializationWindow.SubMessage = "Initialize Error Reporting"; Application.DoEvents();
            ErrorReporter.Initialize(this);

            initializationWindow.SubMessage = "Initializing Right Click Menus"; Application.DoEvents();
            RightClickHelper.Initialize();
            initializationWindow.SubMessage = "Initializing Property Grids"; Application.DoEvents();
            PropertyGridRightClickHelper.Initialize();
            initializationWindow.SubMessage = "Initializing InstructionManager"; Application.DoEvents();
            InstructionManager.Initialize();
            initializationWindow.SubMessage = "Initializing TypeConverter"; Application.DoEvents();
            TypeConverterHelper.InitializeClasses();

            initializationWindow.SubMessage = "Initializing Navigation Stack"; Application.DoEvents();

            initializationWindow.Message = "Loading Glue Settings"; Application.DoEvents();
            // We need to load the glue settings before loading the plugins so that we can 
            // shut off plugins according to settings
            LoadGlueSettings(initializationWindow);


            // Initialize before loading GlueSettings;
            // Also initialize before loading plugins so that plugins
            // can access the standard ATIs
#if GLUE
            string startupPath =
                FileManager.GetDirectory(System.Reflection.Assembly.GetExecutingAssembly().Location);
#else
                string startupPath = FileManager.StartupPath;
#endif
            AvailableAssetTypes.Self.Initialize(startupPath);

            initializationWindow.Message = "Loading Plugins"; Application.DoEvents();
            List<string> pluginsToIgnore = new List<string>();
            if (GlueState.Self.CurrentPluginSettings != null)
            {
                pluginsToIgnore = GlueState.Self.CurrentPluginSettings.PluginsToIgnore;
            }

            PluginManager.SetToolbarTray(this.toolbarControl1);

            // This plugin initialization needs to happen before LoadGlueSettings
            // EVentually we can break this out
            mainExplorerPlugin = new MainExplorerPlugin();
            mainExplorerPlugin.Initialize();

            PluginManager.Initialize(true, pluginsToIgnore);

            ShareUiReferences(PluginCategories.All);

            try
            {
                FileManager.PreserveCase = true;

                initializationWindow.Message = "Initializing File Watch";
                Application.DoEvents();
                // Initialize the FileWatchManager before LoadGlueSettings
                FileWatchManager.Initialize();

                initializationWindow.Message = "Loading Custom Type Info";
                Application.DoEvents();


                Application.DoEvents();
                // Gotta do this too before Loading Glue Settings
                ProjectManager.Initialize();

                initializationWindow.Message = "Loading Project";
                Application.DoEvents();

                // LoadSettings before loading projects
                EditorData.LoadPreferenceSettings();

                while (TaskManager.Self.AreAllAsyncTasksDone == false)
                {
                    System.Threading.Thread.Sleep(100);
                }
                LoadProjectConsideringSettingsAndArgs(initializationWindow);

                // This needs to happen after loading the project:
                ShareUiReferences(PluginCategories.ProjectSpecific);

                Application.DoEvents();
                EditorData.FileAssociationSettings.LoadSettings();

                EditorData.LoadGlueLayoutSettings();

                MainPanelSplitContainer.UpdateSizesFromSettings();

                if (EditorData.GlueLayoutSettings.Maximized)
                    WindowState = FormWindowState.Maximized;


                //ProcessLocations.Initialize();

                ProjectManager.mForm = this;

            }
            catch (Exception exc)
            {
                if (GlueGui.ShowGui)
                {
                    System.Windows.Forms.MessageBox.Show(exc.ToString());

                    FileManager.SaveText(exc.ToString(),
                                         FileManager.UserApplicationDataForThisApplication + "InitError.txt");
                    PluginManager.ReceiveError(exc.ToString());

                    HasErrorOccurred = true;
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                if (GlueGui.ShowGui)
                {
                    initializationWindow.Close();
                    this.BringToFront();
                }
            }
        }

        private void AddErrorReporters()
        {
            EditorObjects.IoC.Container.Get<List<IErrorReporter>>()
                .Add(new CsvErrorReporter());

        }

        private void AddObjectsToIocContainer()
        {
            EditorObjects.IoC.Container.Set(new SetPropertyManager());
            EditorObjects.IoC.Container.Set(new NamedObjectSetVariableLogic());
            EditorObjects.IoC.Container.Set(new StateSaveCategorySetVariableLogic());
            EditorObjects.IoC.Container.Set(new StateSaveSetVariableLogic());
            EditorObjects.IoC.Container.Set(new EventResponseSaveSetVariableLogic());
            EditorObjects.IoC.Container.Set(new ReferencedFileSaveSetPropertyManager());
            EditorObjects.IoC.Container.Set(new CustomVariableSaveSetPropertyLogic());
            EditorObjects.IoC.Container.Set(new EntitySaveSetVariableLogic());
            EditorObjects.IoC.Container.Set(new ScreenSaveSetVariableLogic());
            EditorObjects.IoC.Container.Set(new GlobalContentSetVariableLogic());
            EditorObjects.IoC.Container.Set(new PluginUpdater());

            EditorObjects.IoC.Container.Set<IGlueState>(GlueState.Self);
            EditorObjects.IoC.Container.Set<IGlueCommands>(GlueCommands.Self);

            EditorObjects.IoC.Container.Set<List<IErrorReporter>>(new List<IErrorReporter>());
        }

        private void LoadProjectConsideringSettingsAndArgs(InitializationWindow initializationWindow)
        {
            // This must be called after setting the GlueSettingsSave
            string csprojToLoad;
            ProjectLoader.Self.GetCsprojToLoad(out csprojToLoad);

            if (!string.IsNullOrEmpty(csprojToLoad))
            {
                if (initializationWindow != null)
                {
                    initializationWindow.Message = "Loading " + csprojToLoad;
                }
                LoadProject(csprojToLoad, initializationWindow);
            }
        }

        private void ShareUiReferences(PluginCategories pluginCategories)
        {
            PluginManager.ShareMenuStripReference(mMenu, pluginCategories);

            PluginManager.PrintPreInitializeOutput();
        }

        private void LoadGlueSettings(InitializationWindow initializationWindow)
        {
            string settingsFileLocation = GlueSettingsSave.SettingsFileName;
            if (FileManager.FileExists(settingsFileLocation))
            {
                GlueSettingsSave settingsSave = null;

                bool didErrorOccur = false;

                try
                {
                    settingsSave = FileManager.XmlDeserialize<GlueSettingsSave>(settingsFileLocation);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Error loading your settings file which is located at\n\n" +
                        settingsFileLocation + "\n\nError details:\n\n" + e.ToString());
                    didErrorOccur = true;
                }
                
                if (!didErrorOccur)
                {
                    ProjectManager.GlueSettingsSave = settingsSave;

                    string csprojToLoad;
                    ProjectLoader.Self.GetCsprojToLoad(out csprojToLoad);


                    // Load the plugins settings if it exists
                    string gluxDirectory = null;

                    if (!string.IsNullOrEmpty(csprojToLoad))
                    {
                        gluxDirectory = FileManager.GetDirectory(csprojToLoad);
                    }

                    if (PluginSettings.FileExists(gluxDirectory))
                    {
                        ProjectManager.PluginSettings = PluginSettings.Load(gluxDirectory);
                    }
                    else
                    {
                        ProjectManager.PluginSettings = new PluginSettings();
                    }



                    // attempt to update the positions

                    // This sets the last position, but doesn't work on multiple monitors
                    //this.Left = settingsSave.WindowLeft;
                    //this.Top = settingsSave.WindowTop;

                    // This used to be 0, b
                    this.Height = settingsSave.WindowHeight > 100 ? settingsSave.WindowHeight : 480;
                    this.Width = settingsSave.WindowWidth > 100 ? settingsSave.WindowWidth : 640;
                }
            }
            else
            {
                ProjectManager.GlueSettingsSave.Save();
            }
        }

        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void UpdateGlueSettings()
        {
            GlueSettingsSave save = ProjectManager.GlueSettingsSave;

            string lastFileName = null;

            if (ProjectManager.ProjectBase != null)
            {
                lastFileName = ProjectManager.ProjectBase.FullFileName;
            }

            save.LastProjectFile = lastFileName;

            var glueExeFileName = ProjectLoader.GetGlueExeLocation();
            var foundItem = save.GlueLocationSpecificLastProjectFiles
                .FirstOrDefault(item => item.GlueFileName == glueExeFileName);

            var alreadyIsListed = foundItem != null;

            if(!alreadyIsListed)
            {
                foundItem = new ProjectFileGlueFilePair();
                save.GlueLocationSpecificLastProjectFiles.Add(foundItem);
            }
            foundItem.GlueFileName = glueExeFileName;
            foundItem.GameProjectFileName = lastFileName;
            
            // set up the positions of the window
            save.WindowLeft = this.Left;
            save.WindowTop = this.Top;
            save.WindowHeight = this.Height;
            save.WindowWidth = this.Width;
            save.StoredRecentFiles = this.NumberOfStoredRecentFiles;

            GlueCommands.Self.GluxCommands.SaveSettings();
        }

        public static void CloseProject(bool shouldSave, bool isExiting)
        {
            // Let's set this to true so all tasks can end
            ProjectManager.WantsToClose = true;

            // But give them a chance to end...
            while (TaskManager.Self.AreAllAsyncTasksDone == false)
            {
                // We want to wait until all tasks are done, but
                // if the task is to reload, we can continue or else
                // we'll have a deadlock
                var canContinue = TaskManager.Self.CurrentTask == UpdateReactor.ReloadingProjectDescription ||
                    TaskManager.Self.CurrentTask.StartsWith("Reacting to changed file") && TaskManager.Self.CurrentTask.EndsWith(".glux");

                if(canContinue)
                {
                    break;
                }
                else
                {
                    System.Threading.Thread.Sleep(50);

                    // pump events
                    Application.DoEvents();
                }

            }


            if (shouldSave)
            {
                if (ProjectManager.ProjectBase != null && !string.IsNullOrEmpty(ProjectManager.ProjectBase.FullFileName))
                {
                    GlueCommands.Self.ProjectCommands.SaveProjectsImmediately();
                    Self.UpdateGlueSettings();
                }
            }


            ProjectManager.UnloadProject(isExiting);

            Self.mainExplorerPlugin.HandleProjectClose(isExiting);

            MainGlueWindow.Self.PropertyGrid.SelectedObject = null;

            MainGlueWindow.Self.Text = "FlatRedBall Glue";
            ProjectManager.WantsToClose = false;

        }

        private void FileWatchTimer_Tick(object sender, EventArgs e)
        {
            if(ProjectManager.ProjectBase != null && !string.IsNullOrEmpty(ProjectManager.ProjectBase.FullFileName))
                FileWatchManager.Flush();
        }

        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            ProjectManager.WantsToClose = true;
            MainPanelSplitContainer.ReactToFormClosing();
            
            EditorData.GlueLayoutSettings.BottomPanelSplitterPosition = MainPanelSplitContainer.SplitterDistance;
            EditorData.GlueLayoutSettings.Maximized = this.WindowState == FormWindowState.Maximized;
            EditorData.GlueLayoutSettings.SaveSettings();

            await TaskManager.Self.WaitForAllTasksFinished();

            PluginManager.ReactToGlueClose();
            CloseProject(true, true);
        }

        private void Form1_Activated(object sender, EventArgs e)
        {
            int m = 3;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if(HotkeyManager.Self.TryHandleKeys(keyData))
            {
                return true;
            }
            else
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }
        }
    }
}
