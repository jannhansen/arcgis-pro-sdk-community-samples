﻿//   Copyright 2015 Esri
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at

//       http://www.apache.org/licenses/LICENSE-2.0

//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License. 

using System;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;

namespace ProgressDialog {

    /// <summary>
    /// This Sample shows different patterns that exercise the progress dialog.  
    ///   1. Simple progress dialog
    ///   2. Cancelable progress dialog
    ///   3. Manually control a progress dialog
    /// </summary>
    /// <remarks>
    ///1. Open this solution in Visual Studio 2013.  
    ///2. Save the Project Exploration Tasks.esriTasks file in this solution to a location on your disk.
    ///3. Click the build menu and select Build Solution.
    ///4. Click the Start button to open ArCGIS Pro.  ArcGIS Pro will open.
    ///5. Open any project - it can be an existing project containing data or a new empty project.
    ///6. Click on the Add-in tab and see the split button in the 'Progress Dialog Test' group.
    ///7. Click on any of the split buttons to see the respective progress dialog implementation work.
    /// </remarks>
    internal class ProgressDialogModule : Module {
        private static ProgressDialogModule _this = null;

        /// <summary>
        /// Retrieve the singleton instance to this module here
        /// </summary>
        public static ProgressDialogModule Current {
            get {
                return _this ?? (_this = (ProgressDialogModule)FrameworkApplication.FindModule("ProgressDialog_Module"));
            }
        }

        public static Task RunProgress(ProgressorSource ps, int howLongInSeconds) {
            //simulate doing some work
            //I block the CIM to simulate it being busy
            return QueuedTask.Run(() => Task.Delay(howLongInSeconds*1000).Wait(),  ps.Progressor);
        }

        public static Task RunProgressWithoutBlockingTheCIM(ProgressorSource ps, int howLongInSeconds) {
            //simulate doing some work
            return QueuedTask.Run((Action)(async () => {

                //Here we do NOT block the CIM but...
                //
                //this is probably unrealistic in most cases - that is
                //we await on the CIM so it can do something else
                //while our Task completes.... but, it is shown for completeness
                //
                await Task.Delay(howLongInSeconds * 1000);

            }), ps.Progressor);
        }

        public static Task RunCancelableProgress(CancelableProgressorSource cps, int howLongInSeconds) {
            //simulate doing some work which can be canceled
            return QueuedTask.Run(() => {

                cps.Progressor.Max = (uint)howLongInSeconds;
                //check every second
                while (!cps.Progressor.CancellationToken.IsCancellationRequested) {
                    cps.Progressor.Value += 1;
                    cps.Progressor.Status = "Status " + cps.Progressor.Value;
                    cps.Progressor.Message = "Message " + cps.Progressor.Value;

                    if (System.Diagnostics.Debugger.IsAttached) {
                        System.Diagnostics.Debug.WriteLine(string.Format("RunCancelableProgress Loop{0}", cps.Progressor.Value));
                    }
                    //are we done?
                    if (cps.Progressor.Value == cps.Progressor.Max) break;
                    //block the CIM for a second
                    Task.Delay(1000).Wait();

                }
                System.Diagnostics.Debug.WriteLine(string.Format("RunCancelableProgress: Canceled {0}",
                                                    cps.Progressor.CancellationToken.IsCancellationRequested));

            }, cps.Progressor);

        }

        #region Overrides
        /// <summary>
        /// Called by Framework when ArcGIS Pro is closing
        /// </summary>
        /// <returns>False to prevent Pro from closing, otherwise True</returns>
        protected override bool CanUnload() {
            //TODO - add your business logic
            //return false to ~cancel~ Application close
            return true;
        }

        /// <summary>
        /// Generic implementation of ExecuteCommand to allow calls to
        /// <see cref="FrameworkApplication.ExecuteCommand"/> to execute commands in
        /// your Module.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        protected override Func<Task> ExecuteCommand(string id) {

            //TODO: replace generic implementation with custom logic
            //etc as needed for your Module
            var command = FrameworkApplication.GetPlugInWrapper(id) as ICommand;
            if (command == null)
                return () => Task.FromResult(0);
            if (!command.CanExecute(null))
                return () => Task.FromResult(0);

            return () => {
                command.Execute(null); // if it is a tool, execute will set current tool
                return Task.FromResult(0);
            };
        }
        #endregion Overrides

    }
}
