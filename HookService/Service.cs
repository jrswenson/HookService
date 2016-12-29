using EasyHook;
using HookLib;
using HookRemote;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HookService
{
    public partial class Service : ServiceBase
    {
        static MutexSecurity mutexSecurity = new MutexSecurity();
        static MutexAccessRule newRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.Synchronize, AccessControlType.Allow);
        private static readonly string _win32Dll = typeof(HookManager).Assembly.Location;
        private static readonly string _win64Dll = typeof(HookManager).Assembly.Location;

        static Service()
        {
            mutexSecurity.AddAccessRule(newRule);
        }

        private static string _appGuid = ((GuidAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(GuidAttribute), false).GetValue(0)).Value.ToString();
        private string _runningMutexName = $@"Global\{_appGuid}-{WindowsIdentity.GetCurrent().User.AccountDomainSid}";
        private Mutex _isRunningMutex;
        private bool _hasHandle;

        private string _remoteServiceName = null;
        private OnServerInterface _onServer = new OnServerInterface();

        public Service()
        {
            InitializeComponent();

            bool createdNew;
            _isRunningMutex = new Mutex(false, _runningMutexName, out createdNew, mutexSecurity);
            try
            {
                _hasHandle = _isRunningMutex.WaitOne(1, false);
            }
            catch (AbandonedMutexException)
            {
                _hasHandle = true;
            }
        }

        protected override void OnStart(string[] args)
        {
            StartIt(args);
        }

        protected override void OnStop()
        {
            StopIt();
        }

        public void StartIt(string[] args)
        {            
            RemoteHooking.IpcCreateServer<OnServerInterface>(ref _remoteServiceName, WellKnownObjectMode.SingleCall, _onServer);

            var notePad = Process.GetProcessesByName("Notepad").FirstOrDefault();
            if (notePad != null)
                Inject(notePad.Id);
        }

        public void StopIt()
        {
            try
            {
                if (_isRunningMutex != null && _hasHandle)
                {
                    _isRunningMutex.ReleaseMutex();
                }
            }
            catch (Exception ex)
            {
                //Ignore
            }
            finally
            {
                _isRunningMutex.Close();
                _isRunningMutex = null;
            }
        }

        private void Inject(int id)
        {
            try
            {
                RemoteHooking.Inject(id, InjectionOptions.DoNotRequireStrongName, _win32Dll, _win64Dll, _remoteServiceName, _runningMutexName);
            }
            catch (Exception ex)
            {
                return;
            }
        }
    }
}
