using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DokanXBase;
using DokanXNative;
using Utility;
using Native.Objects;
using ST=System.Threading;
using System.Management;


namespace WinProcfs
{
    class Program
    {
        static void Main(string[] args)
        {
            DokanNative native = new DokanNative(new WinProcFS(),0,"P:","ProcFS","WinProc");
            native.StartDokan();
        }
    }

    class WinProcFS : FileSystemCoreFunctions
    {
        string[] baseinfos ={ "Process.inf", "Network.inf", "Module.inf", "MemRegion.inf", "IO Counters.inf","Window.inf", "Thread.inf" ,"Handle.inf","Heap.inf","Token.inf","EnvVariable.inf"};
        string[] commonSystem={ "Services.inf", "SystemInfo.inf", "Jobs.inf", "EnvVariable.inf","NetStat.inf"};
        ST.Thread processGrabberThread;
        ManagementObjectSearcher processSearcher;
        Dictionary<string, processInfos> database;
       object critical;

       public WinProcFS(){
           database=new Dictionary<string,processInfos>();
           critical=new object();
           processGrabberThread = new ST.Thread(new ST.ThreadStart(this.UpdateProcessData)) ;
           processGrabberThread.Name = "DataProcessor";
           processSearcher = new ManagementObjectSearcher("Select * from Win32_Process");
           processGrabberThread.Start();
       }
       
       public void UpdateProcessData()
       {
           while(true){
           lock(critical)
           {
               string err="";
               database = Native.Objects.Process.EnumerateHiddenProcessesHandleMethod();
               if (err != "")
               {
                   Console.WriteLine("Grabber Error :"+err);
               }
           }
           ST.Thread.Sleep(3000);
           }
       }
       string NullHandler(object str)
       {
           if(str==null)
           return "";

           return str.ToString();

       }
       byte[] FillProcessDetail(int Pid)
       {
           StringBuilder builder=new StringBuilder();
           
          try
          {
          

              processInfos pfs = new processInfos();
              if (database.TryGetValue(Pid.ToString(), out pfs))
              {
                  
                  builder.AppendLine("[Process Property]");
                  builder.AppendLine("AffinityMask=" + NullHandler(pfs.AffinityMask));
                  builder.AppendLine("Name=" + NullHandler(pfs.Name));
                  builder.AppendLine("Path=" + NullHandler(pfs.Path));
                  builder.AppendLine("Priority=" + NullHandler(pfs.Priority));
                  builder.AppendLine("ProcessId=" + NullHandler(pfs.ProcessId));
                  builder.AppendLine("ParentProcessId=" + NullHandler(pfs.ParentProcessId));
                  builder.AppendLine("UserName=" + NullHandler(pfs.UserName));
                  builder.AppendLine("CommandLineArguments=" + NullHandler(pfs.CommandLine));
                  builder.AppendLine("DomainName" + NullHandler(pfs.DomainName));

                  builder.AppendLine("");
                  builder.AppendLine("[Perfomance and Resouces]");
                  builder.AppendLine("AverageCpuUsage=" + NullHandler(pfs.AverageCpuUsage * 100));
                  builder.AppendLine("ProcessorTime=" + NullHandler(pfs.ProcessorTime));
                  builder.AppendLine("UserModeTime" + NullHandler(pfs.UserTime));
                  builder.AppendLine("KernelTime=" + NullHandler(pfs.KernelTime));
                  builder.AppendLine("StartTime=" + NullHandler(pfs.StartTime));

                  builder.AppendLine("");
                  builder.AppendLine("\n[System Resources Count]");
                  builder.AppendLine("ThreadCount=" + NullHandler(pfs.ThreadCount));
                  builder.AppendLine("UserObjectsCount=" + NullHandler(pfs.UserObjects));
                  builder.AppendLine("GdiObjects=" + NullHandler(pfs.GdiObjects));
                  builder.AppendLine("HandleCounts=" + NullHandler(pfs.HandleCount));
                  
                  builder.AppendLine("");
                  builder.AppendLine("[ProcessMisc]");
                  builder.AppendLine("HasReanalize=" + NullHandler(pfs.HasReanalize));
                  builder.AppendLine("IsHidden=" + NullHandler(pfs.IsHidden));
                  Console.WriteLine(builder.ToString());
              }
             

              
          }
          catch(Exception e)
          {
              Console.WriteLine("Info Get:"+e.Message);              
          }
          return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
       }

       byte[] FillMemorykDetail(int Pid)
       {
           StringBuilder builder = new StringBuilder();

           try
           {
              
               processInfos pfs = new processInfos();
               if (database.TryGetValue(Pid.ToString(), out pfs))
               {

                   builder.AppendLine("[Memory Count]");
                   builder.AppendLine("PageFaultCount=" + NullHandler(pfs.MemoryInfos.PageFaultCount));
                   builder.AppendLine("PagefileUsage=" + NullHandler(pfs.MemoryInfos.PagefileUsage));
                   builder.AppendLine("PeakPagefileUsage=" + NullHandler(pfs.MemoryInfos.PeakPagefileUsage));
                   builder.AppendLine("PeakVirtualSize=" + NullHandler(pfs.MemoryInfos.PeakVirtualSize));
                   builder.AppendLine("PeakWorkingSetSize=" + NullHandler(pfs.MemoryInfos.PeakWorkingSetSize));
                   builder.AppendLine("PrivateBytes=" + NullHandler(pfs.MemoryInfos.PrivateBytes));
                   builder.AppendLine("VirtualSize=" + NullHandler(pfs.MemoryInfos.VirtualSize));
                   builder.AppendLine("WorkingSetSize=" + NullHandler(pfs.MemoryInfos.WorkingSetSize));
                   

                   builder.AppendLine("");
                   builder.AppendLine("[Quota Details]");
                   builder.AppendLine("QuotaPeakPagedPoolUsage=" + NullHandler(pfs.MemoryInfos.QuotaPeakPagedPoolUsage));
                   builder.AppendLine("QuotaNonPagedPoolUsage=" + NullHandler(pfs.MemoryInfos.QuotaNonPagedPoolUsage));
                   builder.AppendLine("QuotaPagedPoolUsage=" + NullHandler(pfs.MemoryInfos.QuotaPagedPoolUsage));
                   builder.AppendLine("QuotaPeakNonPagedPoolUsage" + NullHandler(pfs.MemoryInfos.QuotaPeakNonPagedPoolUsage));
                   
      
                   Console.WriteLine(builder.ToString());
               }

  

           }
           catch (Exception e)
           {
               Console.WriteLine("Info Get:" + e.Message);
           }
           return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
       }

       byte[] FillNetworkStatDetail()
       {
           StringBuilder builder = new StringBuilder();

           try
           {               
               Native.Api.NativeStructs.MibTcpStats tcpState= Native.Objects.Network.GetTcpStatistics();
               Native.Api.NativeStructs.MibUdpStats udp = Native.Objects.Network.GetUdpStatistics();

              
               builder.AppendLine("[TCPStats]");
               builder.AppendLine("ActiveOpens=" + NullHandler(tcpState.ActiveOpens));
               builder.AppendLine("AttemptFails=" + NullHandler(tcpState.AttemptFails));
               builder.AppendLine("CurrEstab=" + NullHandler(tcpState.CurrEstab));
               builder.AppendLine("EstabResets=" + NullHandler(tcpState.EstabResets));
               builder.AppendLine("InErrs=" + NullHandler(tcpState.InErrs));
               builder.AppendLine("InSegs=" + NullHandler(tcpState.InSegs));
               builder.AppendLine("MaxConn=" + NullHandler(tcpState.MaxConn));
               builder.AppendLine("NumConns=" + NullHandler(tcpState.NumConns));
               builder.AppendLine("OutRsts=" + NullHandler(tcpState.OutRsts));
               builder.AppendLine("PassiveOpens=" + NullHandler(tcpState.PassiveOpens));
               builder.AppendLine("RetransSegs=" + NullHandler(tcpState.RetransSegs));
               builder.AppendLine("RtoAlgorithm=" + NullHandler(tcpState.RtoAlgorithm));
               builder.AppendLine("RtoMax=" + NullHandler(tcpState.RtoMax));
               builder.AppendLine("RtoMin=" + NullHandler(tcpState.RtoMin));
               
               builder.AppendLine("");

               builder.AppendLine("[UDPStats]");
               builder.AppendLine("InDatagrams=" + NullHandler(udp.InDatagrams));
               builder.AppendLine("InErrors=" + NullHandler(udp.InErrors));
               builder.AppendLine("NoPorts=" + NullHandler(udp.NoPorts));
               builder.AppendLine("NumAddrs=" + NullHandler(udp.NumAddrs));
               builder.AppendLine("OutDatagrams=" + NullHandler(udp.OutDatagrams));              

               Console.WriteLine(builder.ToString());

           }
           catch (Exception e)
           {
               Console.WriteLine("Info Get:" + e.Message);
           }
           return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
       } 

       byte[] FillNetworkkDetail(int Pid)
       {
           StringBuilder builder = new StringBuilder();

           try
           {
              Dictionary<string,networkInfos> SocketInfo=new Dictionary<string,networkInfos>();
              Native.Objects.Network.EnumerateTcpUdpConnections(ref SocketInfo,true,Pid);
               processInfos pfs = new processInfos();              
               if(SocketInfo.Values.Count>=1)
               {
                   builder.AppendLine("[Network Connection]");
                   foreach (networkInfos info in SocketInfo.Values)
                   {

                       if (info.ProcessId == Pid)
                       {
                           builder.AppendLine("Protocol=" + NullHandler(info.Protocol));
                           builder.AppendLine("State=" + NullHandler(info.State));
                           builder.AppendLine("LocalAddress=" + NullHandler(info.Local));
                           
                           builder.AppendLine("RemoteAdress=" + NullHandler(info.Remote));
                                                      
                           builder.AppendLine("");
                       }
                           
                       
                   }

                  
               }

               Console.WriteLine(builder.ToString());

           }
           catch (Exception e)
           {
               Console.WriteLine("Info Get:" + e.Message);
           }
           return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
       } 
 
        public uint Def_Cleanup(string filename, IntPtr info)
        {
            return 0;
        }

        public uint Def_CloseFile(string filename, IntPtr info)
        {
            return 0;
        }
        public uint Def_CreateDirectory(string filename, IntPtr info)
        {
            
            return NtStatus.MediaWriteProtected;
        }

        public uint Def_CreateFile(string filename, System.IO.FileAccess access, System.IO.FileShare share, System.IO.FileMode mode, System.IO.FileOptions options, IntPtr info)
        {
            VirtualNode node = new VirtualNode(filename);
            if (node.isFile)
            {
                if (!node.CurrentNodeFile.EndsWith("inf",StringComparison.CurrentCultureIgnoreCase))
                {
                    return 0;
                }
                else
                    return NtStatus.ObjectPathNotFound;

            }
           
            return 0;
        }

        public uint Def_ReadFile(string filename, IntPtr buffer, uint BufferSize, ref uint NumberByteReadSuccess, long Offset, IntPtr info)
        {
            try
            {
                byte[] file=null;
                VirtualNode Node = new VirtualNode(filename);
                Console.WriteLine("{0},{1},{2}", Node.CurrentNodeDir, Node.CurrentNodeFile,Node.isFile); 
                if (Node.isFile)
                {
                    Console.WriteLine("{0} {1}", Node.CurrentNodeDir, Node.CurrentNodeFile);
                    if (Node.CurrentNodeFile.EndsWith(".inf"))
                    {
                       
                        int pid;
                        Console.WriteLine("OK ....");
                        if (int.TryParse(Node.CurrentNodeDir, out pid))
                        {

                            Console.WriteLine("ID case "+pid);
                            switch (Node.CurrentNodeFile) { 
                                case "Process.inf":
                                file = FillProcessDetail(pid);
                                    break;
                                case "Memory.inf":
                                    file = FillMemorykDetail(pid);
                                    break;
                                case "Network.inf":
                                    file = FillNetworkkDetail(pid);
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            Console.WriteLine("ID " + pid);
                            if(Node.CurrentNodeDir=="\\" || Node.CurrentNodeDir==""){
                               
                           switch (Node.CurrentNodeFile) 
                            {
                                case "NetStat.inf":
                                file = FillNetworkStatDetail();
                                break;
                                default:
                                break;
                            }
                            }


                        }
                        if (file != null && file.Length != 0 && Offset < file.Length)
                        {
                            if (BufferSize > file.Length - Offset)
                            {
                                NumberByteReadSuccess = (uint)(file.Length - Offset);
                                System.Runtime.InteropServices.Marshal.Copy(file, (int)Offset, buffer, (int)NumberByteReadSuccess);
                            }
                            else
                            {
                                NumberByteReadSuccess = BufferSize;
                                System.Runtime.InteropServices.Marshal.Copy(file, (int)Offset, buffer, (int)BufferSize);
                            }
                        }
                        else {
                            NumberByteReadSuccess = 0;
                        }

                        
                    }

                }
                return 0;
            }
            catch(Exception e)
            {
                Console.WriteLine("Read -"+e.Message+" Line "+new System.Diagnostics.StackTrace(e,true).GetFrame(0).GetFileLineNumber());
                return NtStatus.FileInvalid;
            }
        }

    
        public uint Def_GetDiskInfo(ref ulong Available, ref ulong Total, ref ulong Free)
        {
            Available = 95245862458;
            Free = 95245862458;
            Total = 152458624580;
            return 0;
        }

        public uint Def_GetFileInformation(string filename, ref BY_HANDLE_FILE_INFORMATION Information, IntPtr info)
        {
            VirtualNode Node=new VirtualNode(filename);
            Information.CreationTime = HelperFunction.DateTimeToFileTime(DateTime.Now.ToFileTime());
            Information.LastAccessTime = HelperFunction.DateTimeToFileTime(DateTime.Now.ToFileTime());
            Information.LastWriteTime = HelperFunction.DateTimeToFileTime(DateTime.Now.ToFileTime());
            Information.FileSizeLow = 2000;
            if (Node.isFile)
                Information.FileAttributes = FileAttributes.Readonly;
            else
                Information.FileAttributes = FileAttributes.Directory|FileAttributes.Readonly;

            return 0;
        }

        public uint Def_GetVolumeInfo(IntPtr VolumeNameBuffer, uint VolumeNameSize, ref uint SerialNumber, ref uint MaxComponenetLegnth, ref uint FileSystemFeatures, IntPtr FileSystemNameBuffer, uint FileSystemNameSize)
        {
            HelperFunction.SetVolumeInfo(VolumeNameBuffer, "Process Data", (int)VolumeNameSize, FileSystemNameBuffer, "ProcFS", (int)FileSystemNameSize);
            SerialNumber = (uint)GetHashCode();
            MaxComponenetLegnth = 256;
           
            return 0;
        }

        public uint Def_DeleteDirectory(string filename, IntPtr info)
        {
            return NtStatus.MediaWriteProtected;
        }

        public uint Def_DeleteFile(string filename, IntPtr info)
        {
            return NtStatus.MediaWriteProtected;
        }

        public uint Def_FindFiles(string filename, ref FillFindData FillFunction, IntPtr info)
        {
            try
            {
                VirtualNode Node = new VirtualNode(filename);
                if ((Node.RootNode == "\\" || Node.RootNode == "") && !Node.isFile && Node.CurrentNodeDir=="")
                {
                    lock (critical)
                    {
                        foreach (string prcs in database.Keys)
                        {
                            WIN32_FIND_DATA Information = new WIN32_FIND_DATA();
                            Information.cFileName = prcs;
                            Information.ftCreationTime = HelperFunction.DateTimeToFileTime(DateTime.Now.ToFileTime());
                            Information.ftLastAccessTime = HelperFunction.DateTimeToFileTime(DateTime.Now.ToFileTime());
                            Information.ftLastWriteTime = HelperFunction.DateTimeToFileTime(DateTime.Now.ToFileTime());
                            Information.dwFileAttributes = FileAttributes.Directory | FileAttributes.Readonly;
                            FillFunction(ref Information, info);
                        }
                    }
                    foreach (string file in commonSystem)
                    {
                        WIN32_FIND_DATA Information = new WIN32_FIND_DATA();
                        Information.cFileName = file;
                        Information.ftCreationTime = HelperFunction.DateTimeToFileTime(DateTime.Now.ToFileTime());
                        Information.ftLastAccessTime = HelperFunction.DateTimeToFileTime(DateTime.Now.ToFileTime());
                        Information.ftLastWriteTime = HelperFunction.DateTimeToFileTime(DateTime.Now.ToFileTime());
                        Information.dwFileAttributes = FileAttributes.Readonly;
                        Information.nFileSizeLow = 2000;
                        FillFunction(ref Information, info);
                    }

                }
                else if (!Node.isFile && Node.CurrentNodeDir != "" && Node.RootNode!="")
                {
                    int id;
                    if (Node.CurrentNodeDir != "" && int.TryParse(Node.CurrentNodeDir, out id))
                    {
                        List<int> childProcess = Process.EnumerateChildProcessesById(id);
                        foreach (int prcs in childProcess)
                        {
                            WIN32_FIND_DATA Information = new WIN32_FIND_DATA();
                            Information.cFileName = prcs.ToString();
                            Information.ftCreationTime = HelperFunction.DateTimeToFileTime(DateTime.Now.ToFileTime());
                            Information.ftLastAccessTime = HelperFunction.DateTimeToFileTime(DateTime.Now.ToFileTime());
                            Information.ftLastWriteTime = HelperFunction.DateTimeToFileTime(DateTime.Now.ToFileTime());
                            Information.dwFileAttributes = FileAttributes.Directory | FileAttributes.Readonly;                            
                            FillFunction(ref Information, info);
                        }
                        foreach (string file in baseinfos)
                        {
                            WIN32_FIND_DATA Information = new WIN32_FIND_DATA();
                            Information.cFileName = file;
                            Information.ftCreationTime = HelperFunction.DateTimeToFileTime(DateTime.Now.ToFileTime());
                            Information.ftLastAccessTime = HelperFunction.DateTimeToFileTime(DateTime.Now.ToFileTime());
                            Information.ftLastWriteTime = HelperFunction.DateTimeToFileTime(DateTime.Now.ToFileTime());
                            Information.nFileSizeLow = 2000;
                            Information.dwFileAttributes = FileAttributes.Readonly;
                            FillFunction(ref Information, info);
                        }


                    }
                }

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("Find Error :"+ e.Message+"  Line:"+new System.Diagnostics.StackTrace(e,true).GetFrame(0).GetFileLineNumber());
                return NtStatus.FileInvalid;
            }
        }

        public uint Def_FlushFileBuffers(string filename, IntPtr info)
        {
            return 0;
        }

        
        public uint Def_LockFile(string filename, long offset, long length, IntPtr info)
        {
            return 0;
        }

        public uint Def_MoveFile(string filename, string newname, bool replace, IntPtr info)
        {
            return NtStatus.MediaWriteProtected;
        }

        public uint Def_OpenDirectory(string filename, IntPtr info)
        {
            return 0;
        }

       

        public uint Def_SetAllocationSize(string filename, long length, IntPtr info)
        {
            return NtStatus.MediaWriteProtected; ;
        }

        public uint Def_SetEndOfFile(string filename, long length, IntPtr info)
        {
            return NtStatus.MediaWriteProtected;
        }

        public uint Def_SetFileAttributes(string filename, uint Attribute, IntPtr info)
        {
            return NtStatus.MediaWriteProtected; ;
        }

        public uint Def_SetFileTime(string filename, System.Runtime.InteropServices.ComTypes.FILETIME ctime, System.Runtime.InteropServices.ComTypes.FILETIME atime, System.Runtime.InteropServices.ComTypes.FILETIME mtime, IntPtr info)
        {
            return NtStatus.MediaWriteProtected;
        }

        public uint Def_UnlockFile(string filename, long offset, long length, IntPtr info)
        {
            return NtStatus.MediaWriteProtected;
        }

        public uint Def_Unmount(IntPtr info)
        {
            return 0;
        }

        public uint Def_WriteFile(string filename, byte[] buffer, ref uint writtenBytes, long offset, IntPtr info)
        {
            return NtStatus.MediaWriteProtected;
        }
    }
}
