﻿using CompilerLibrary.Models;
using FlatRedBall.Glue.MVVM;
using System;
using System.Windows;

namespace GameCommunicationPlugin.GlueControl.ViewModels
{
    public class GlueViewSettingsViewModel : ViewModel
    {
        public bool EnableLiveEdit
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool RestartOnFailedCommands
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool EmbedGameInGameTab
        {
            get => Get<bool>();
            set => Set(value);
        }

        public int PortNumber
        {
            get => Get<int>();
            set => Set(value);
        }

        public bool ShowScreenBoundsWhenViewingEntities
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool SetBackgroundColor
        {
            get => Get<bool>();
            set => Set(value);
        }

        public int BackgroundRed
        {
            get => Get<int>();
            set => Set(value);
        }

        public int BackgroundGreen
        {
            get => Get<int>();
            set => Set(value);
        }

        public int BackgroundBlue
        {
            get => Get<int>();
            set => Set(value);
        }

        public bool ShowGrid
        {
            get => Get<bool>();
            set => Set(value);
        }

        public decimal GridAlpha
        {
            get => Get<decimal>();
            set => Set(value);
        }

        public decimal GridSize
        {
            get => Get<decimal>();
            set
            {
                const decimal minValue = 4;
                value = Math.Max(value, minValue);
                Set(value);
            }
        }

        public bool RestartScreenOnLevelContentChange
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool EnableSnapping
        {
            get => Get<bool>();
            set => Set(value);
        }


        public decimal SnapSize
        {
            get => Get<decimal>();
            set => Set(value);
        }

        public decimal PolygonPointSnapSize
        {
            get => Get<decimal>();
            set => Set(value);
        }

        [DependsOn(nameof(EnableLiveEdit))]
        public Visibility ShowWindowDefenderUi => EnableLiveEdit.ToVisibility();

        public GlueViewSettingsViewModel()
        {
            RestartOnFailedCommands = true;
        }


        internal void SetFrom(CompilerSettingsModel model)
        {
            this.PortNumber = model.PortNumber;
            this.RestartOnFailedCommands = model.RestartOnFailedCommands;
            this.ShowScreenBoundsWhenViewingEntities = model.ShowScreenBoundsWhenViewingEntities;

            this.ShowGrid = model.ShowGrid;
            this.GridAlpha = model.GridAlpha;
            this.GridSize = model.GridSize;
            this.SetBackgroundColor = model.SetBackgroundColor;
            this.BackgroundRed = model.BackgroundRed;
            this.BackgroundGreen = model.BackgroundGreen;
            this.BackgroundBlue = model.BackgroundBlue;
            this.EnableLiveEdit = model.GenerateGlueControlManagerCode;
            this.EmbedGameInGameTab = model.EmbedGameInGameTab;
            this.RestartScreenOnLevelContentChange = model.RestartScreenOnLevelContentChange;

            this.EnableSnapping = model.EnableSnapping;
            this.SnapSize = model.SnapSize;
            this.PolygonPointSnapSize = model.PolygonPointSnapSize;
        }

        internal void SetModel(CompilerSettingsModel compilerSettings)
        {
            compilerSettings.PortNumber = this.PortNumber;
            compilerSettings.RestartOnFailedCommands = this.RestartOnFailedCommands;
            compilerSettings.ShowScreenBoundsWhenViewingEntities = this.ShowScreenBoundsWhenViewingEntities;

            compilerSettings.ShowGrid = this.ShowGrid;
            compilerSettings.GridAlpha = this.GridAlpha;
            compilerSettings.GridSize = this.GridSize;

            compilerSettings.SetBackgroundColor = this.SetBackgroundColor;
            compilerSettings.BackgroundRed = this.BackgroundRed;
            compilerSettings.BackgroundGreen = this.BackgroundGreen;
            compilerSettings.BackgroundBlue = this.BackgroundBlue;


            compilerSettings.GenerateGlueControlManagerCode = this.EnableLiveEdit;
            compilerSettings.EmbedGameInGameTab = this.EmbedGameInGameTab;
            compilerSettings.RestartScreenOnLevelContentChange = this.RestartScreenOnLevelContentChange;

            compilerSettings.EnableSnapping = this.EnableSnapping ;
            compilerSettings.SnapSize = this.SnapSize;
            compilerSettings.PolygonPointSnapSize = this.PolygonPointSnapSize;

            compilerSettings.ToolbarObjects.Clear();
        }
    }
}
