﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurpleSharp.Simulations
{
    public class DefenseEvasion
    {

        public static void ClearSecurityEventLogCmd(string log)
        {
            ExecutionHelper.StartProcess("", "wevtutil.exe cl Security", log);

        }

        public static void ClearSecurityEventLog(string log)
        {
            string currentPath = AppDomain.CurrentDomain.BaseDirectory;
            Lib.Logger logger = new Lib.Logger(currentPath + log);

            logger.TimestampInfo(String.Format("Starting Security Event Log clearing Simulation on {0}", Environment.MachineName));

            try
            {
                EventLog eventlog = new EventLog();
                eventlog.Source = "Security";
                eventlog.Clear();
                eventlog.Close();
                logger.TimestampInfo(String.Format("Succesffully cleared the Security EventLog"));
            }
            catch (Exception ex)
            {
                logger.TimestampInfo(String.Format("Failed to clear the Security EventLog"));
                logger.TimestampInfo(ex.Message.ToString());
            }

        }

        public static void ProcessInjection(string log)
        {
            string currentPath = AppDomain.CurrentDomain.BaseDirectory;
            Lib.Logger logger = new Lib.Logger(currentPath + log);
            logger.TimestampInfo(String.Format("Starting Process Injection Simulation on {0}", Environment.MachineName));

            try
            {

                Process proc = new Process();
                proc.StartInfo.FileName = "C:\\Windows\\system32\\notepad.exe";
                proc.StartInfo.UseShellExecute = false;
                proc.Start();
                logger.TimestampInfo(String.Format("Trying to inject to {0} PID: {1}", proc.ProcessName, proc.Id));
                //DefenseEvasionHelper.ProcInjection_CreateRemoteThread(Convert.FromBase64String(Lib.Static.donut_ping), proc);
                DefenseEvasionHelper.ProcInjection_APC(Convert.FromBase64String(Lib.Static.donut_ping), proc);

                //DefenseEvasionHelper.ProcInjection_CreateRemoteThread(Lib.Static.msf_meter, not);
                logger.TimestampInfo(String.Format("Success!"));
            }
            catch ( Exception ex)
            {
                logger.TimestampInfo(String.Format("Error!"));
                logger.TimestampInfo(String.Format(ex.ToString()));
                logger.TimestampInfo(String.Format(ex.Message.ToString()));
            }
            
        }


    }
}
