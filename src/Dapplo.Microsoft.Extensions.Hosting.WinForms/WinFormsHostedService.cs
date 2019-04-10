﻿//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2019 Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Microsoft.Extensions.Hosting
// 
//  Dapplo.Microsoft.Extensions.Hosting is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Microsoft.Extensions.Hosting is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Microsoft.Extensions.Hosting. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dapplo.Microsoft.Extensions.Hosting.WinForms
{
    /// <summary>
    /// This hosts a WinForms service, making sure the lifecycle is managed
    /// </summary>
    public class WinFormsHostedService : IHostedService
    {
        private readonly ILogger<WinFormsHostedService> _logger;
        private readonly IApplicationLifetime _applicationLifetime;
        private readonly IWinFormsContext _winFormsContext;
        private readonly Form _shell;

        /// <summary>
        /// The constructor which takes all the DI objects
        /// </summary>
        /// <param name="logger">ILogger</param>
        /// <param name="applicationLifetime"></param>
        /// <param name="winFormsContext">IWinFormsContext</param>
        /// <param name="winFormsShell">IWinFormsShell optional</param>
        public WinFormsHostedService(ILogger<WinFormsHostedService> logger, IApplicationLifetime applicationLifetime, IWinFormsContext winFormsContext, IWinFormsShell winFormsShell = null)
        {
            _logger = logger;
            _applicationLifetime = applicationLifetime;
            _winFormsContext = winFormsContext;
            _shell = winFormsShell as Form;
        }

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_winFormsContext.EnableVisualStyles)
            {
                Application.EnableVisualStyles();
            }
            // Register to the host application lifetime ApplicationStopping to shutdown the WinForms application
            _applicationLifetime.ApplicationStopping.Register(()  =>
            {
                if (_winFormsContext.IsRunning)
                {
                    _logger.LogDebug("Stopping WinForms application.");
                    _winFormsContext.FormsDispatcher.Invoke(Application.Exit);
                }
            });

            // Register to the WinForms application exit to stop the host application
            Application.ApplicationExit += (s,e) =>
            {
                _winFormsContext.IsRunning = false;
                if (_winFormsContext.IsLifetimeLinked)
                {
                    _logger.LogDebug("Stopping host application due to WinForms application exit.");
                    _applicationLifetime.StopApplication();
                }
            };


            // Run the application
            _winFormsContext.FormsDispatcher.Invoke(() =>
            {
                _winFormsContext.IsRunning = true;
                if (_shell != null)
                {
                    Application.Run(_shell);
                }
                else
                {
                    Application.Run();
                }
            });
            
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _winFormsContext.FormsDispatcher.Invoke(Application.Exit);
            return Task.CompletedTask;
        }
    }
}