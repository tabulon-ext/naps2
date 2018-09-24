﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Windows.Forms;
using NAPS2.ClientServer;
using NAPS2.DI.Modules;
using NAPS2.Util;
using NAPS2.WinForms;
using NAPS2.Worker;
using Ninject;

namespace NAPS2.DI.EntryPoints
{
    /// <summary>
    /// The entry point for NAPS2.Server.exe, which exposes scanning devices over the network to other NAPS2 applications.
    /// </summary>
    public static class ServerEntryPoint
    {
        private const int DEFAULT_PORT = 33277;

        public static void Run(string[] args)
        {
            try
            {
                // Initialize Ninject (the DI framework)
                var kernel = new StandardKernel(new CommonModule(), new WinFormsModule());

                // Start a pending worker process
                WorkerManager.Init();

                // Set up basic application configuration
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.ThreadException += UnhandledException;

                // Set up a form for the server process
                var form = new BackgroundForm();
                Invoker.Current = form;

                int port = DEFAULT_PORT;
                foreach (var portArg in args.Where(a => a.StartsWith("/Port:", StringComparison.OrdinalIgnoreCase)))
                {
                    if (int.TryParse(portArg.Substring(6), out int parsedPort))
                    {
                        port = parsedPort;
                    }
                }

                // Listen for requests
                using (var host = new ServiceHost(typeof(ScanService)))
                {
                    var serverIcon = new ServerNotifyIcon(port, () => form.Close());
                    serverIcon.Show();
                    host.Description.Behaviors.Add(new ServiceFactoryBehavior(() => kernel.Get<ScanService>()));
                    host.AddServiceEndpoint(typeof(IScanService),
                        new NetTcpBinding {ReceiveTimeout = TimeSpan.FromHours(1), SendTimeout = TimeSpan.FromHours(1)}, $"net.tcp://0.0.0.0:{port}/NAPS2.Server");
                    host.Open();
                    Application.Run(form);
                }
            }
            catch (Exception ex)
            {
                Log.FatalException("An error occurred that caused the server application to close.", ex);
                Environment.Exit(1);
            }
        }

        private static void UnhandledException(object sender, ThreadExceptionEventArgs threadExceptionEventArgs)
        {
            Log.FatalException("An error occurred that caused the server to close.", threadExceptionEventArgs.Exception);
        }
    }
}
