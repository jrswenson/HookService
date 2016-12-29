using EasyHook;
using HookLib.Properties;
using HookRemote;
using HookWinUser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HookLib
{
    public class HookManager : IEntryPoint
    {
        private OnServerInterface _server = null;
        private AutoResetEvent _waitForIt = new AutoResetEvent(false);

        public HookManager(RemoteHooking.IContext inContext, string InChannelName, string mutexName)
        {
            _server = RemoteHooking.IpcConnectClient<OnServerInterface>(InChannelName);

            var mainHwnd = Process.GetCurrentProcess().MainWindowHandle;
        }

        public void Run(RemoteHooking.IContext InContext, string InChannelName, string mutexName)
        {
            var origIcon = default(IntPtr);
            origIcon = Externals.SendMessage(Process.GetCurrentProcess().MainWindowHandle, Constants.WM_GETICON, new IntPtr(Constants.ICON_SMALL), IntPtr.Zero);

            var connectedIcon = Resources.Connected;
            Externals.SendMessage(Process.GetCurrentProcess().MainWindowHandle, Constants.WM_SETICON, new IntPtr(Constants.ICON_SMALL), connectedIcon.Handle);

            try
            {
                Task.Factory.StartNew(() =>
                {
                    var mutex = Mutex.OpenExisting(mutexName, MutexRights.Synchronize);
                    try
                    {
                        var res = mutex.WaitOne(Timeout.Infinite, false);
                        if (res)
                            mutex.ReleaseMutex();
                    }
                    catch (AbandonedMutexException ex)
                    {
                        var msg = ex.Message;
                        //Ignore
                    }
                    finally
                    {
                        mutex.Close();
                        mutex = null;
                        _waitForIt.Set();
                    }
                });

                _waitForIt.WaitOne();
            }
            finally
            {
                //Do hook clean up.
                Externals.SendMessage(Process.GetCurrentProcess().MainWindowHandle, Constants.WM_SETICON, new IntPtr(Constants.ICON_SMALL), origIcon);
            }
        }
    }
}
