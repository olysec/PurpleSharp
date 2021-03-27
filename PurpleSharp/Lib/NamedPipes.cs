﻿using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Management;
using System.Linq;
using System.Text;
using System.Security.Principal;
using System.Security.AccessControl;
using Newtonsoft.Json;
using System.Threading;

namespace PurpleSharp.Lib
{
    class NamedPipes
    {

        //Based on https://github.com/malcomvetter/NamedPipes
        public static void RunScoutService(string scout_np, string simulator_np, string log)
        {
            string currentPath = AppDomain.CurrentDomain.BaseDirectory;
            Logger logger = new Logger(currentPath + log);
            bool running = true;
            bool privileged = false;

            string technique, opsec, simpfath, simrpath, duser, user, simbinary, cleanup;
            technique = opsec = simpfath = simrpath = duser = user = simbinary = cleanup = "";
            Process parentprocess = null;
            int pbsleep, tsleep, variation;
            pbsleep = tsleep = 0;
            variation = 1;
            System.Threading.Thread.Sleep(1500);

            try
            {
                using (var pipeServer = new NamedPipeServerStream(scout_np, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message))
                {
                    logger.TimestampInfo("Starting scout namedpipe service with PID:"+ Process.GetCurrentProcess().Id);
                    while (running)
                    {
                        var reader = new StreamReader(pipeServer);
                        var writer = new StreamWriter(pipeServer);

                        //logger.TimestampInfo("Waiting for client connection...");
                        pipeServer.WaitForConnection();
                        //logger.TimestampInfo("Client connected!");

                        var line = reader.ReadLine();

                        logger.TimestampInfo("Received from client: " + line);

                        if (line.ToLower().Equals("syn"))
                        {
                            //logger.TimestampInfo("sending back to client: " + "SYN/ACK");
                            writer.WriteLine("SYN/ACK");
                            writer.Flush();
                        }
                        else if (line.ToLower().Equals("auditpol"))
                        {
                            writer.WriteLine(System.Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(Recon.GetAuditPolicy())));
                            writer.Flush();
                        }
                        else if (line.ToLower().Equals("wef"))
                        {
                            writer.WriteLine(System.Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(Recon.GetWefSettings())));
                            writer.Flush();
                        }
                        else if (line.ToLower().Equals("pws"))
                        {
                            writer.WriteLine(System.Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(Recon.GetPwsLoggingSettings())));
                            writer.Flush();
                        }
                        else if (line.ToLower().Equals("ps"))
                        {
                            writer.WriteLine(System.Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(Recon.GetProcs())));
                            writer.Flush();
                        }
                        else if (line.ToLower().Equals("svcs"))
                        {
                            writer.WriteLine(System.Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(Recon.GetServices())));
                            writer.Flush();
                        }
                        else if (line.ToLower().Equals("cmdline"))
                        {
                            writer.WriteLine(System.Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(Recon.GetCmdlineAudittingSettings())));
                            writer.Flush();
                        }

                        else if (line.ToLower().StartsWith("recon:"))
                        {
                            string payload = "";
                            if (line.Replace("recon:", "").Equals("privileged")) privileged = true;
                            parentprocess = Recon.GetHostProcess(privileged);
                            if (parentprocess != null && Recon.GetExplorer() != null)
                            {
                                duser = Recon.GetProcessOwnerWmi(Recon.GetExplorer());
                                user = duser.Split('\\')[1];
                                logger.TimestampInfo(String.Format("Recon identified {0} logged in. Process to Spoof: {1} PID: {2}", duser, parentprocess.ProcessName, parentprocess.Id));
                                payload = String.Format("{0},{1},{2},{3}", duser, parentprocess.ProcessName, parentprocess.Id, privileged.ToString());

                            }
                            else
                            {
                                payload = ",,,";
                                logger.TimestampInfo("Recon did not identify any logged users");
                            }
                            writer.WriteLine(payload);
                            writer.Flush();
                        }
                        else if (line.ToLower().StartsWith("sc:"))
                        {
                            writer.WriteLine("ACK");
                            writer.Flush();
                        }
                        else if (line.ToLower().StartsWith("technique:"))
                        {
                            technique = line.Replace("technique:", "");
                            //logger.TimestampInfo("Got params from client");
                            //logger.TimestampInfo("sending back to client: " + "ACK");
                            writer.WriteLine("ACK");
                            writer.Flush();
                        }
                        else if (line.ToLower().StartsWith("variation:"))
                        {
                            variation = Int32.Parse(line.Replace("variation:", ""));
                            //logger.TimestampInfo("Got params from client");
                            //logger.TimestampInfo("sending back to client: " + "ACK");
                            writer.WriteLine("ACK");
                            writer.Flush();
                        }
                        else if (line.ToLower().StartsWith("pbsleep:"))
                        {
                            pbsleep = Int32.Parse(line.Replace("pbsleep:", ""));
                            //logger.TimestampInfo("Got params from client");
                            //logger.TimestampInfo("sending back to client: " + "ACK");
                            writer.WriteLine("ACK");
                            writer.Flush();
                        }
                        else if (line.ToLower().StartsWith("tsleep:"))
                        {
                            tsleep = Int32.Parse(line.Replace("tsleep:", ""));
                            //logger.TimestampInfo("Got params from client");
                            //logger.TimestampInfo("sending back to client: " + "ACK");
                            writer.WriteLine("ACK");
                            writer.Flush();
                        }
                        else if (line.ToLower().StartsWith("opsec:"))
                        {
                            opsec = line.Replace("opsec:", "");
                            //logger.TimestampInfo("Got opsec technique from client");
                            //logger.TimestampInfo("sending back to client: " + "ACK");
                            writer.WriteLine("ACK");
                            writer.Flush();
                        }
                        else if (line.ToLower().StartsWith("cleanup:"))
                        {
                            cleanup = line.Replace("cleanup:", "");
                            writer.WriteLine("ACK");
                            writer.Flush();
                        }
                        else if (line.ToLower().StartsWith("simrpath:"))
                        {
                            simrpath = line.Replace("simrpath:", "");
                            //logger.TimestampInfo("sending back to client: " + "ACK");
                            //simpath = "C:\\Users\\" + loggeduser + "\\Downloads\\" + simbin;
                            simpfath = "C:\\Users\\" + user + "\\" + simrpath;
                            int index = simrpath.LastIndexOf(@"\");
                            simbinary = simrpath.Substring(index + 1);

                            writer.WriteLine("ACK");
                            writer.Flush();
                        }
                        else if (line.Equals("act"))
                        {
                            logger.TimestampInfo("Received act!");
                            //logger.TimestampInfo("sending back to client: " + "ACK");
                            writer.WriteLine("ACK");
                            writer.Flush();

                            if (opsec.Equals("ppid"))
                            {
                                logger.TimestampInfo("Using Parent Process Spoofing technique for Opsec");
                                logger.TimestampInfo("Spoofing " + parentprocess.ProcessName + " PID: " + parentprocess.Id.ToString());
                                logger.TimestampInfo("Executing: " + simpfath + " /n");
                                //Launcher.SpoofParent(parentprocess.Id, simpath, simbin + " " + cmdline);
                                //Launcher.SpoofParent(parentprocess.Id, simpfath, simrpath + " /s");

                                Launcher.SpoofParent(parentprocess.Id, simpfath, simbinary + " /n");
                                //Launcher.SpoofParent(parentprocess.Id, simpfath, simbinary + " /s");

                                System.Threading.Thread.Sleep(3000);
                                logger.TimestampInfo("Sending payload to Simulation Agent through namedpipe: " + "technique:" + technique + " pbsleep:" + pbsleep.ToString() + " tsleep:" + tsleep.ToString() + " cleanup:" + cleanup);
                                RunNoAuthClient(simulator_np, "technique:" + technique +" variation:"+ variation.ToString() + " pbsleep:" + pbsleep.ToString() + " tsleep:"+tsleep.ToString() + " cleanup:" + cleanup);
                                System.Threading.Thread.Sleep(2000);
                            }
                        }
                        else if (line.ToLower().Equals("quit"))
                        {
                            logger.TimestampInfo("Received quit! Exitting namedpipe");
                            //logger.TimestampInfo("sending back to client: " + "quit");
                            writer.WriteLine("quit");
                            writer.Flush();
                            running = false;
                        }
                        pipeServer.Disconnect();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.TimestampInfo(ex.ToString());
                logger.TimestampInfo(ex.Message.ToString());
            }
        }


        public static void RunScoutServiceSerialized(string scout_np, string simulator_np, string log)
        {
            string currentPath = AppDomain.CurrentDomain.BaseDirectory;
            Logger logger = new Logger(currentPath + log);
            bool running = true;
            bool privileged = false;

            string technique, opsec, simpfath, duser, user, simbinary, cleanup;
            technique = opsec = simpfath =  duser = user = simbinary = cleanup = "";
            Process parentprocess = null;
            int pbsleep, tsleep, variation;
            pbsleep = tsleep = 0;
            variation = 1;
            SimulationRequestPayload s_payload = null;
            Thread.Sleep(1500);

            try
            {
                using (var pipeServer = new NamedPipeServerStream(scout_np, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message))
                {

                    logger.TimestampInfo("Starting scout namedpipe service with PID:" + Process.GetCurrentProcess().Id);
                    while (running)
                    {
                        SimulationResponse sim_response;
                        logger.TimestampInfo("Waiting for client connection...");
                        pipeServer.WaitForConnection();
                        logger.TimestampInfo("Client connected.");
                        var messageBytes = ReadMessage(pipeServer);
                        var line = Encoding.UTF8.GetString(messageBytes);
                        logger.TimestampInfo("Received from client: " + line);
                        SimulationRequest sim_request = JsonConvert.DeserializeObject<SimulationRequest>(line);

                        if (sim_request.header.Equals("SYN"))
                        {
                            logger.TimestampInfo("Received SYN");
                            s_payload = sim_request.sim_request_payload;
                            ReconResponse recon_response;
                            if (sim_request.sim_request_payload.recon_type.Equals("privileged")) privileged = true;
                            parentprocess = Recon.GetHostProcess(privileged);
                            if (parentprocess != null && Recon.GetExplorer() != null)
                            {
                                duser = Recon.GetProcessOwnerWmi(Recon.GetExplorer());
                                recon_response = new ReconResponse(duser, parentprocess.ProcessName, parentprocess.Id.ToString(), privileged.ToString());
                                user = duser.Split('\\')[1];
                                logger.TimestampInfo(String.Format("Recon identified {0} logged in. Process to Spoof: {1} PID: {2}", duser, parentprocess.ProcessName, parentprocess.Id));
                            }
                            else
                            {
                                recon_response = new ReconResponse("", "", "", "");
                                logger.TimestampInfo("Recon did not identify any logged users");
                            }

                            simpfath = "C:\\Users\\" + user + "\\" + sim_request.sim_request_payload.simulator_rpath;
                            int index = sim_request.sim_request_payload.simulator_rpath.LastIndexOf(@"\");
                            simbinary = sim_request.sim_request_payload.simulator_rpath.Substring(index + 1);

                            sim_response = new SimulationResponse("SYN/ACK", recon_response);
                            byte[] bytes_sim_response = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sim_response));
                            pipeServer.Write(bytes_sim_response, 0, bytes_sim_response.Length);
                            logger.TimestampInfo(String.Format("Sent SimulationResponse object"));

                        }
                        else if (sim_request.header.Equals("ACT"))
                        {

                            logger.TimestampInfo("Received ACT");
                            if (s_payload.opsec.Equals("ppid"))
                            {

                                logger.TimestampInfo("Using Parent Process Spoofing technique for Opsec");
                                logger.TimestampInfo("Spoofing " + parentprocess.ProcessName + " PID: " + parentprocess.Id.ToString());
                                logger.TimestampInfo("Executing: " + simpfath + " /n");
                                //Launcher.SpoofParent(parentprocess.Id, simpath, simbin + " " + cmdline);
                                //Launcher.SpoofParent(parentprocess.Id, simpfath, simrpath + " /s");

                                Launcher.SpoofParent(parentprocess.Id, simpfath, simbinary + " /n");
                                //Launcher.SpoofParent(parentprocess.Id, simpfath, simbinary + " /s");

                                logger.TimestampInfo("Sending payload to Simulation Agent through namedpipe: " + "technique:" + s_payload.techniques + " pbsleep:" + s_payload.playbook_sleep + " tsleep:" + s_payload.task_sleep + " cleanup:" + s_payload.cleanup);

                                byte[] bytes_sim_rqeuest = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new SimulationRequest("ACK", s_payload)));
                                string result = NamedPipes.RunNoAuthClientSerialized(simulator_np, bytes_sim_rqeuest);
                                logger.TimestampInfo("Received back from Simulator " + result);
 
                            }

                            sim_response = new SimulationResponse("ACK");
                            byte[] bytes_sim_response = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sim_response));
                            pipeServer.Write(bytes_sim_response, 0, bytes_sim_response.Length);
                            logger.TimestampInfo(String.Format("Sent SimulationResponse object 2"));
                            running = false;

                        }
                        else if (sim_request.header.Equals("FIN"))
                        {
                            logger.TimestampInfo("Received a FIN command");
                            sim_response = new SimulationResponse("ACK");
                            byte[] bytes_sim_response = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sim_response));
                            pipeServer.Write(bytes_sim_response, 0, bytes_sim_response.Length);
                            running = false;
                        }

                        pipeServer.Disconnect();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.TimestampInfo(ex.ToString());
                logger.TimestampInfo(ex.Message.ToString());
            }
        }

        public static void RunScoutServiceSerialized2(string scout_np, string simulator_np, string log)
        {
            string currentPath = AppDomain.CurrentDomain.BaseDirectory;
            Logger logger = new Logger(currentPath + log);
            bool running = true;
            bool privileged = false;

            string technique, opsec, simpfath, duser, user, simbinary, cleanup;
            technique = opsec = simpfath = duser = user = simbinary = cleanup = "";
            Process parentprocess = null;
            int pbsleep, tsleep, variation;
            pbsleep = tsleep = 0;
            variation = 1;
            SimulationPlaybook PlaybookToSend = null;
            Thread.Sleep(1500);

            try
            {
                using (var pipeServer = new NamedPipeServerStream(scout_np, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message))
                {

                    logger.TimestampInfo("Starting scout namedpipe service with PID:" + Process.GetCurrentProcess().Id);
                    while (running)
                    {
                        SimulationResponse sim_response;
                        logger.TimestampInfo("Waiting for client connection...");
                        pipeServer.WaitForConnection();
                        logger.TimestampInfo("Client connected.");
                        var messageBytes = ReadMessage(pipeServer);
                        var line = Encoding.UTF8.GetString(messageBytes);
                        logger.TimestampInfo("Received from client: " + line);
                        SimulationRequest2 sim_request = JsonConvert.DeserializeObject<SimulationRequest2>(line);

                        if (sim_request.header.Equals("SYN"))
                        {
                            logger.TimestampInfo("Received SYN");
                            PlaybookToSend = sim_request.playbook;
                            ReconResponse recon_response;
                            if (sim_request.recon_type.Equals("privileged")) privileged = true;
                            parentprocess = Recon.GetHostProcess(privileged);
                            if (parentprocess != null && Recon.GetExplorer() != null)
                            {
                                duser = Recon.GetProcessOwnerWmi(Recon.GetExplorer());
                                recon_response = new ReconResponse(duser, parentprocess.ProcessName, parentprocess.Id.ToString(), privileged.ToString());
                                user = duser.Split('\\')[1];
                                logger.TimestampInfo(String.Format("Recon identified {0} logged in. Process to Spoof: {1} PID: {2}", duser, parentprocess.ProcessName, parentprocess.Id));
                            }
                            else
                            {
                                recon_response = new ReconResponse("", "", "", "");
                                logger.TimestampInfo("Recon did not identify any logged users");
                            }

                            simpfath = "C:\\Users\\" + user + "\\" + sim_request.playbook.simulator_relative_path;
                            int index = sim_request.playbook.simulator_relative_path.LastIndexOf(@"\");
                            simbinary = sim_request.playbook.simulator_relative_path.Substring(index + 1);

                            sim_response = new SimulationResponse("SYN/ACK", recon_response);
                            byte[] bytes_sim_response = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sim_response));
                            pipeServer.Write(bytes_sim_response, 0, bytes_sim_response.Length);
                            logger.TimestampInfo(String.Format("Sent SimulationResponse object"));

                        }
                        else if (sim_request.header.Equals("ACT"))
                        {

                            logger.TimestampInfo("Received ACT");
                            if (PlaybookToSend.opsec.Equals("ppid"))
                            {

                                logger.TimestampInfo("Using Parent Process Spoofing technique for Opsec");
                                logger.TimestampInfo("Spoofing " + parentprocess.ProcessName + " PID: " + parentprocess.Id.ToString());
                                logger.TimestampInfo("Executing: " + simpfath + " /n");
                                //Launcher.SpoofParent(parentprocess.Id, simpath, simbin + " " + cmdline);
                                //Launcher.SpoofParent(parentprocess.Id, simpfath, simrpath + " /s");

                                Launcher.SpoofParent(parentprocess.Id, simpfath, simbinary + " /n");
                                //Launcher.SpoofParent(parentprocess.Id, simpfath, simbinary + " /s");

                                //logger.TimestampInfo("Sending payload to Simulation Agent through namedpipe: " + "technique:" + s_payload.techniques + " pbsleep:" + s_payload.playbook_sleep + " tsleep:" + s_payload.task_sleep + " cleanup:" + s_payload.cleanup);
                                logger.TimestampInfo("Sending Simulation Playbook to Simulation Agent through namedpipe: " + PlaybookToSend.simulator_relative_path);

                                byte[] bytes_sim_rqeuest = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new SimulationRequest2("ACK", "", PlaybookToSend)));
                                string result = NamedPipes.RunNoAuthClientSerialized(simulator_np, bytes_sim_rqeuest);
                                logger.TimestampInfo("Received back from Simulator " + result);
                            }

                            sim_response = new SimulationResponse("ACK");
                            byte[] bytes_sim_response = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sim_response));
                            pipeServer.Write(bytes_sim_response, 0, bytes_sim_response.Length);
                            logger.TimestampInfo(String.Format("Sent SimulationResponse object 2"));
                            running = false;

                        }
                        else if (sim_request.header.Equals("FIN"))
                        {
                            logger.TimestampInfo("Received a FIN command");
                            sim_response = new SimulationResponse("ACK");
                            byte[] bytes_sim_response = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sim_response));
                            pipeServer.Write(bytes_sim_response, 0, bytes_sim_response.Length);
                            running = false;
                        }

                        pipeServer.Disconnect();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.TimestampInfo(ex.ToString());
                logger.TimestampInfo(ex.Message.ToString());
            }
        }

        private static byte[] ReadMessage(PipeStream pipe)
        {
            byte[] buffer = new byte[1024];
            using (var ms = new MemoryStream())
            {
                do
                {
                    var readBytes = pipe.Read(buffer, 0, buffer.Length);
                    ms.Write(buffer, 0, readBytes);
                }
                while (!pipe.IsMessageComplete);

                return ms.ToArray();
            }
        }


        public static string[] RunSimulationService(string npipe, string log)
        {
            string[] result = new string[5];
            try
            {
                //https://helperbyte.com/questions/171742/how-to-connect-to-a-named-pipe-without-administrator-rights
                PipeSecurity ps = new PipeSecurity();
                ps.SetAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null), PipeAccessRights.ReadWrite, AccessControlType.Allow));

                //logger.TimestampInfo("starting!");
                string technique, pbsleep, tsleep, cleanup, variation;
                using (var pipeServer = new NamedPipeServerStream(npipe, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous, 4028, 4028, ps))
                
                {
                    var reader = new StreamReader(pipeServer);
                    var writer = new StreamWriter(pipeServer);

                    pipeServer.WaitForConnection();
                    var line = reader.ReadLine();
                
                    if (line.ToLower().StartsWith("technique:"))
                    {
                        string[] options = line.Split(' ');
                        technique = options[0].Replace("technique:", "");
                        variation = options[1].Replace("variation:", "");
                        pbsleep = options[2].Replace("pbsleep:", "");
                        tsleep = options[3].Replace("tsleep:", "");
                        cleanup = options[4].Replace("cleanup:", "");
                        writer.WriteLine("ACK");
                        writer.Flush();

                        result[0] = technique;
                        result[1] = variation;
                        result[2] = pbsleep;
                        result[3] = tsleep;
                        result[4] = cleanup;
                        return result;
                    }
                    pipeServer.Disconnect();
                }
                return result;
            }
            catch
            {
                return result;
            }

        }

        public static SimulationPlaybook RunSimulationServiceSerialized(string npipe, string log)
        {
            string[] result = new string[5];
            SimulationPlaybook playbook = null;
            string currentPath = AppDomain.CurrentDomain.BaseDirectory;
            Logger logger = new Logger(currentPath + log);
            try
            {
                //https://helperbyte.com/questions/171742/how-to-connect-to-a-named-pipe-without-administrator-rights
                PipeSecurity ps = new PipeSecurity();
                ps.SetAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null), PipeAccessRights.ReadWrite, AccessControlType.Allow));

                logger.TimestampInfo("starting Simulator!");
                string technique, pbsleep, tsleep, cleanup, variation;
                using (var pipeServer = new NamedPipeServerStream(npipe, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous, 4028, 4028, ps))

                {
                    SimulationResponse sim_response;
                    logger.TimestampInfo("Waiting for client connection...");
                    pipeServer.WaitForConnection();
                    logger.TimestampInfo("Client connected.");
                    var messageBytes = ReadMessage(pipeServer);
                    var line = Encoding.UTF8.GetString(messageBytes);
                    logger.TimestampInfo("Received from client: " + line);
                    SimulationRequest2 sim_request = JsonConvert.DeserializeObject<SimulationRequest2>(line);

                    playbook = sim_request.playbook;

                    /*
                    result[0] = sim_request.sim_request_payload.techniques;
                    result[1] = sim_request.sim_request_payload.variation;
                    result[2] = sim_request.sim_request_payload.playbook_sleep;
                    result[3] = sim_request.sim_request_payload.task_sleep;
                    result[4] = sim_request.sim_request_payload.cleanup;
                    */

                    sim_response = new SimulationResponse("ACK");
                    byte[] bytes_sim_response = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sim_response));
                    pipeServer.Write(bytes_sim_response, 0, bytes_sim_response.Length);


                    pipeServer.Disconnect();
                    return playbook;
                }
            }
            catch
            {
                return playbook;
            }

        }

        //Based on https://github.com/malcomvetter/NamedPipes
        public static string RunClient(string rhost, string domain, string ruser, string rpwd, string npipe, string request)
        {
            using (new Impersonation(domain, ruser, rpwd))
            {
                using (var pipeClient = new NamedPipeClientStream(rhost, npipe, PipeDirection.InOut))
                {
                    pipeClient.Connect(100000);
                    pipeClient.ReadMode = PipeTransmissionMode.Message;

                    var reader = new StreamReader(pipeClient);
                    var writer = new StreamWriter(pipeClient);
                    writer.WriteLine(request);
                    writer.Flush();                                        
                    var result = reader.ReadLine();
                    return (result.ToString());
                    

                }
            }
        }

        public static string RunClientSerialized(string rhost, string domain, string ruser, string rpwd, string npipe, byte[] serialized_object)
        {
            using (new Impersonation(domain, ruser, rpwd))
            {
                using (var pipeClient = new NamedPipeClientStream(rhost, npipe, PipeDirection.InOut))
                {
                    pipeClient.Connect(100000);
                    pipeClient.ReadMode = PipeTransmissionMode.Message;

                    var reader = new StreamReader(pipeClient);
                    //var writer = new StreamWriter(pipeClient);
                    var writer2 = new BinaryWriter(pipeClient);
                    writer2.Write(serialized_object);
                    var restul1 = reader.ReadLine();
                    return (restul1.ToString());
                }
            }
        }

        public static string RunNoAuthClient(string npipe, string request)
        {
            using (var pipeClient = new NamedPipeClientStream(".", npipe, PipeDirection.InOut))
            {
                pipeClient.Connect(10000);
                pipeClient.ReadMode = PipeTransmissionMode.Message;

                var reader = new StreamReader(pipeClient);
                var writer = new StreamWriter(pipeClient);
                writer.WriteLine(request);
                writer.Flush();
                var result = reader.ReadLine();
                return (result.ToString());
            } 
        }

        public static string RunNoAuthClientSerialized(string npipe, byte[] serialized_object)
        {
            using (var pipeClient = new NamedPipeClientStream(".", npipe, PipeDirection.InOut))
            {
                pipeClient.Connect(10000);
                pipeClient.ReadMode = PipeTransmissionMode.Message;

                var reader = new StreamReader(pipeClient);
                var writer2 = new BinaryWriter(pipeClient);
                writer2.Write(serialized_object);
                //var writer = new StreamWriter(pipeClient);
                //writer.WriteLine(request);
                //writer.Flush();
                var result = reader.ReadLine();
                return (result.ToString());
            }
        }
    }
}
