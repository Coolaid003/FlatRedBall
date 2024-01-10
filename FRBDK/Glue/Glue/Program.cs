﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using FlatRedBall;
using FlatRedBall.Glue;
using System.Runtime.Remoting;
using System.Threading;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GlueFormsCore.Controls;

namespace Glue;

static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // This is a semi-hack to fix blurry rendering issues with controls on some systems. This likely
        // sacrifices perf for stability. See:
        // https://github.com/vchelaru/FlatRedBall/issues/151
        System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;

        // Add proper exception handling so we can handle plugin exceptions:
        CreateExceptionHandlingEvents();

        ApplicationConfiguration.Initialize();


        var mainGlueWindow =
            new MainGlueWindow();
        try
        {
            Application.Run(mainGlueWindow);
        }
        catch (Exception e)
        {
            if (!MainPanelControl.IsExiting)
            {
                MessageBox.Show(e.ToString());
            }
        }

    }

    private static void CreateExceptionHandlingEvents()
    {
        // Add the event handler for handling UI thread exceptions to the event.
        Application.ThreadException += new ThreadExceptionEventHandler(UIThreadException);

        // Set the unhandled exception mode to force all Windows Forms errors to go through
        // our handler.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        // Add the event handler for handling non-UI thread exceptions to the event. 
        AppDomain.CurrentDomain.UnhandledException +=
            new UnhandledExceptionEventHandler(UnhandledException);
    }

    private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        HandleExceptionsUnified(e.ExceptionObject);
    }

    private static void HandleExceptionsUnified(object objectToPrint)
    {
        bool wasPluginError = PluginManager.TryHandleException(objectToPrint as Exception);
        if (!wasPluginError)
        {
            GlueCommands.Self.PrintError(objectToPrint?.ToString());
        }
    }

    private static void UIThreadException(object sender, ThreadExceptionEventArgs e)
    {
        HandleExceptionsUnified(e.Exception);
    }


}


