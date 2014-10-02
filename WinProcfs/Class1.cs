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
using System.Runtime.InteropServices;
using SD=System.Diagnostics;
using System.Globalization;


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
        string[] baseinfos = { "Process.inf", "Network.inf", "Module.inf", "MemRegion.inf", "IO Counters.inf", "Window.inf", "Thread.inf", "Handle.inf", "Heap.inf", "Token.inf", "EnvVariable.inf" };
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

           private static bool IsWin64(int pid)
        {
               try{
            System.Diagnostics.Process process=  System.Diagnostics.Process.GetProcessById(pid);
            if ((Environment.OSVersion.Version.Major > 5)
                || ((Environment.OSVersion.Version.Major == 5) && (Environment.OSVersion.Version.Minor >= 1)))
            {
                        try
                        {
                            bool retVal;

                            return NativeMethods.IsWow64Process(process.Handle, out retVal) && retVal;
                        }
                        catch
                        {
                            return false; // access is denied to the process
                        }
                    }

                        return false; // not on 64-bit Windows
             }
               catch (Exception e)
               {
                   return false;
               }

        

    }

       internal static class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWow64Process([In] IntPtr process, [Out] out bool wow64Process);
    }

       private bool ProcessExists(int id)
        {
            return SD.Process.GetProcesses().Any(x => x.Id == id);
        }

       byte[] FillJobDetail()
       {
           StringBuilder builder = new StringBuilder();
           try
           {
                     Dictionary<string, jobInfos> info = Job.EnumerateJobs(); ;
                 
                   foreach (jobInfos minf in info.Values)
                   {
                       builder.AppendLine("[Job " + minf.Name + "]");
                       builder.AppendLine("ActiveProcess= " + NullHandler(minf.BasicAndIoAccountingInformation.BasicInfo.ActiveProcesses));
                       builder.AppendLine("TotalKernelTime= " + NullHandler(minf.BasicAndIoAccountingInformation.BasicInfo.ThisPeriodTotalKernelTime));
                       builder.AppendLine("TotalUserTime= " + NullHandler(minf.BasicAndIoAccountingInformation.BasicInfo.ThisPeriodTotalUserTime));
                       builder.AppendLine("KernelTime= " + NullHandler(minf.BasicAndIoAccountingInformation.BasicInfo.TotalKernelTime));
                       builder.AppendLine("TotalPageFaultCount= " + NullHandler(minf.BasicAndIoAccountingInformation.BasicInfo.TotalPageFaultCount));
                       builder.AppendLine("TotalProcess= " + NullHandler(minf.BasicAndIoAccountingInformation.BasicInfo.TotalProcesses));
                       builder.AppendLine("TotalTeminatedProcess= " + NullHandler(minf.BasicAndIoAccountingInformation.BasicInfo.TotalTerminatedProcesses));
                       builder.AppendLine("TotalUserTime= " + NullHandler(minf.BasicAndIoAccountingInformation.BasicInfo.TotalUserTime));
                       builder.AppendLine("ActiveProcessLimit=" + NullHandler(minf.BasicLimitInformation.ActiveProcessLimit));
                       builder.AppendLine("Affinity=" + NullHandler(minf.BasicLimitInformation.Affinity));
                       builder.AppendLine("LimitFlags=" + NullHandler(minf.BasicLimitInformation.LimitFlags));
                       builder.AppendLine("MaximumWorkingSetSize=" + NullHandler(minf.BasicLimitInformation.MaximumWorkingSetSize));
                       builder.AppendLine("MinimumWorkingSetSize=" + NullHandler(minf.BasicLimitInformation.MinimumWorkingSetSize));
                       builder.AppendLine("PerJobUserTimeLimit=" + NullHandler(minf.BasicLimitInformation.PerJobUserTimeLimit));
                       builder.AppendLine("PerProcessTimeLimit=" + NullHandler(minf.BasicLimitInformation.PerProcessUserTimeLimit));
                       builder.AppendLine("PriorityClass=" + NullHandler(minf.BasicLimitInformation.PriorityClass));
                       builder.AppendLine("SchedulingClass=" + NullHandler(minf.BasicLimitInformation.SchedulingClass));
                       builder.AppendLine("Name=" + NullHandler(minf.Name));
                       builder.AppendLine("PidList=" + NullHandler(minf.PidList.Select(i => i.ToString(CultureInfo.InvariantCulture)).Aggregate((s1, s2) => s1 + ", " + s2)));                       
                        builder.AppendLine("");
                   }

                   Console.WriteLine(builder.ToString());
               



           }
           catch (Exception e)
           {
               builder.AppendLine(e.Message);
               Console.WriteLine("Info Get:" + e.Message);

           }
           return System.Text.Encoding.UTF8.GetBytes(builder.ToString());

       } 

       byte[] FillSysDetail()
       {
           StringBuilder builder = new StringBuilder();

           try
           {


               Native.Api.NativeStructs.SystemInfo info = SystemInfo.GetSystemInfo();
               
                       builder.AppendLine("[System:" + Environment.MachineName + "]");

                       builder.AppendLine("ActiveProcessorMask:" + NullHandler(info.dwActiveProcessorMask));
                       builder.AppendLine("AllocationGranularity:" + NullHandler(info.dwAllocationGranularity));
                       builder.AppendLine("PageSize:" + NullHandler(info.dwPageSize));
                       builder.AppendLine("NumberOfProcessors:" + NullHandler(info.dwNumberOfProcessors));
                       builder.AppendLine("ProcessorLevel:" + NullHandler(info.dwProcessorLevel));
                       builder.AppendLine("ProcessorRevision:" + NullHandler(info.dwProcessorRevision));
                       builder.AppendLine("ProcessorType:" + NullHandler(info.dwProcessorType));
                       builder.AppendLine("MaximumApplicationAddress:" + NullHandler(info.lpMaximumApplicationAddress));
                       builder.AppendLine("MinimumApplicationAddress" + NullHandler(info.lpMinimumApplicationAddress));
                       Console.WriteLine(builder.ToString());
           }
           catch (Exception e)
           {
               builder.AppendLine(e.Message);
               Console.WriteLine("Info Get:" + e.Message);

           }
           return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
       }
            
       byte[] FillServiceDetail()
       {
           StringBuilder builder = new StringBuilder();

           try
           {
               
               processInfos pfs = new processInfos();
           

                   Dictionary<string, serviceInfos> srv = new Dictionary<string, serviceInfos>();
                   Service.EnumerateServices(Service.GetSCManagerHandle(Native.Security.ServiceManagerAccess.EnumerateService), ref srv, true, true);
                   foreach (serviceInfos minf in srv.Values)
                   {
                       builder.AppendLine("[MemoryRegion:" + minf.DisplayName + "]");
                       builder.AppendLine("AcceptedControl=" + NullHandler(minf.AcceptedControl));
                       builder.AppendLine("CheckPoint=" + NullHandler(minf.CheckPoint));
                       builder.AppendLine("Dependencies=" + NullHandler(minf.Dependencies));
                       builder.AppendLine("Desription=" + NullHandler(minf.Description));
                       builder.AppendLine("DigonosticMessageFile=" + NullHandler(minf.DiagnosticMessageFile));
                       builder.AppendLine("ErrorControl=" + NullHandler(minf.ErrorControl));                       
                       builder.AppendLine("ImagePath=" + NullHandler(minf.ImagePath));
                       builder.AppendLine("LoadOrderGroup=" + NullHandler(minf.LoadOrderGroup));
                       builder.AppendLine("Name=" + NullHandler(minf.Name));
                       builder.AppendLine("ObjectName=" + NullHandler(minf.ObjectName));
                       builder.AppendLine("ProcessId=" + NullHandler(minf.ProcessId));
                       builder.AppendLine("ProcessName=" + NullHandler(minf.ProcessName));
                       builder.AppendLine("ServiceFlag=" + NullHandler(minf.ServiceFlags));
                       builder.AppendLine("ServiceSpecificExitCode=" + NullHandler(minf.ServiceSpecificExitCode));
                       builder.AppendLine("ServiceStartName=" + NullHandler(minf.ServiceStartName));
                       builder.AppendLine("ServiceType=" + NullHandler(minf.ServiceType));
                       builder.AppendLine("StartType=" + NullHandler(minf.StartType));
                       builder.AppendLine("State=" + NullHandler(minf.State));
                       builder.AppendLine("Tag=" + NullHandler(minf.Tag));
                       builder.AppendLine("TagID=" + NullHandler(minf.TagID));
                       builder.AppendLine("WaitHint=" + NullHandler(minf.WaitHint));
                       builder.AppendLine("Win32ExitCode=" + NullHandler(minf.Win32ExitCode));
                       builder.AppendLine(NullHandler(minf.FileInfo));
                       builder.AppendLine("");

                       
                   

                   Console.WriteLine(builder.ToString());
               }



           }
           catch (Exception e)
           {
               builder.AppendLine(e.Message);
               Console.WriteLine("Info Get:" + e.Message);

           }
           return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
       }

       byte[] FillMemRegionDetail(int Pid)
       {
           StringBuilder builder = new StringBuilder();

           try
           {


               processInfos pfs = new processInfos();
               if (database.TryGetValue(Pid.ToString(), out pfs))
               {

                   
                   Dictionary<string,memRegionInfos> info=new Dictionary<string,memRegionInfos>();
                   MemRegion.EnumerateMemoryRegionsByProcessId(Pid, ref info);
                   foreach (memRegionInfos minf in info.Values)
                   {
                       builder.AppendLine("[MemoryRegion "+minf.Name+"]");
                       builder.AppendLine("BaseAddress=" + NullHandler(minf.BaseAddress));
                       builder.AppendLine("Protection=" + NullHandler(minf.Protection));
                       builder.AppendLine("RegionSize="+NullHandler(minf.RegionSize));
                       builder.AppendLine("State=" + NullHandler(minf.State));
                       builder.AppendLine("Type=" + NullHandler(minf.Type));
                       builder.AppendLine("");
                   }
           
                   Console.WriteLine(builder.ToString());
               }



           }
           catch (Exception e)
           {
               builder.AppendLine(e.Message);
               Console.WriteLine("Info Get:" + e.Message);

           }
           return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
       }

       byte[] FillIODetail(int Pid)
       {
           StringBuilder builder = new StringBuilder();

           try
           {


               processInfos pfs = new processInfos();
               
               if (database.TryGetValue(Pid.ToString(), out pfs))
               {

                   
                  
                       builder.AppendLine("[IOCounter]");
                       builder.AppendLine("WriteOperationCount=" + NullHandler(pfs.IOValues.WriteOperationCount));
                       builder.AppendLine("WriteTransferCount=" + NullHandler(pfs.IOValues.WriteTransferCount));
                       builder.AppendLine("ReadOperationCount=" + NullHandler(pfs.IOValues.ReadOperationCount));
                       builder.AppendLine("ReadTransferCount=" + NullHandler(pfs.IOValues.ReadTransferCount));
                       builder.AppendLine("OtherOperationCount=" + NullHandler(pfs.IOValues.OtherOperationCount));
                       builder.AppendLine("OtherTransferCount=" + NullHandler(pfs.IOValues.OtherTransferCount));
                       builder.AppendLine("");
               

                   Console.WriteLine(builder.ToString());
               }



           }
           catch (Exception e)
           {
               builder.AppendLine(e.Message);
               Console.WriteLine("Info Get:" + e.Message);

           }
           return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
       }

       byte[] FillWindowDetail(int Pid)
       {
           StringBuilder builder = new StringBuilder();

           try
           {


               processInfos pfs = new processInfos();
               Dictionary<string, windowInfos> win = new Dictionary<string, windowInfos>();
               Window.EnumerateWindowsByProcessId(Pid,false,true,ref win,true);

               foreach(windowInfos info in win.Values)
               {
                   builder.AppendLine("[Windows ]");
                   builder.AppendLine("Enabled=" + NullHandler(info.Enabled));
                   builder.AppendLine("Handle=" + NullHandler(info.Handle));
                   builder.AppendLine("Height=" + NullHandler(info.Height));
                   builder.AppendLine("Width=" + NullHandler(info.Width));
                   builder.AppendLine("IsTask=" + NullHandler(info.IsTask));
                   builder.AppendLine("Top=" + NullHandler(info.Top));
                   builder.AppendLine("Left=" + NullHandler(info.Left));
                   builder.AppendLine("Opacity=" + NullHandler(info.Opacity));
                   builder.AppendLine("ThreadId=" + NullHandler(info.ThreadId));
                   builder.AppendLine("Visible=" + NullHandler(info.Visible));
                   builder.AppendLine("");
                   
               }
               Console.WriteLine(builder.ToString());


           }
           catch (Exception e)
           {
               builder.AppendLine(e.Message);
               Console.WriteLine("Info Get:" + e.Message);

           }
           return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
       }
       byte[] FillThreadDetail(int Pid)
       {
           StringBuilder builder = new StringBuilder();

           try
           {


               processInfos pfs = new processInfos();
               Dictionary<string, threadInfos> thr = new Dictionary<string, threadInfos>();
               Thread.EnumerateThreadsByProcessId(ref thr, Pid);

               foreach (threadInfos info in thr.Values)
               {
                   builder.AppendLine("[Thread "+info.Id+" ]");
                   builder.AppendLine("KernelTime=" + NullHandler(info.KernelTime));
                   builder.AppendLine("Priority=" + NullHandler(info.Priority));
                   builder.AppendLine("ProcessID=" + NullHandler(info.ProcessId));
                   builder.AppendLine("StartAddress=" + NullHandler(info.StartAddress));
                   builder.AppendLine("State=" + NullHandler(info.State));
                   builder.AppendLine("TotalTime=" + NullHandler(info.TotalTime));
                   builder.AppendLine("UserTime=" + NullHandler(info.UserTime));
                   builder.AppendLine("WaitReason=" + NullHandler(info.WaitReason));
                   builder.AppendLine("WaitTime=" + NullHandler(info.WaitTime));                  
                   builder.AppendLine("");

               }
               Console.WriteLine(builder.ToString());


           }
           catch (Exception e)
           {
               builder.AppendLine(e.Message);
               Console.WriteLine("Info Get:" + e.Message);

           }
           return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
       }

       byte[] FillHandleDetail(int Pid)
       {
           StringBuilder builder = new StringBuilder();

           try
           {


               processInfos pfs = new processInfos();
               Dictionary<string, handleInfos> hnd = new Dictionary<string, handleInfos>();
               Handle.EnumerateHandleByProcessId(Pid, true, ref hnd);

               foreach (handleInfos info in hnd.Values)
               {
                   builder.AppendLine("[Handle]");
                   builder.AppendLine("Attribute=" + NullHandler(info.Attributes));
                   builder.AppendLine("CreateTime=" + NullHandler(info.CreateTime));
                   builder.AppendLine("GrantedAccess=" + NullHandler(info.GrantedAccess));
                   builder.AppendLine("Handle=" + NullHandler(info.Handle));
                   builder.AppendLine("HandleCount=" + NullHandler(info.HandleCount));
                   builder.AppendLine("Key=" + NullHandler(info.Key));
                   builder.AppendLine("Name=" + NullHandler(info.Name));
                   builder.AppendLine("NonPagedPoolUsage=" + NullHandler(info.NonPagedPoolUsage));
                   builder.AppendLine("ObjectAddress=" + NullHandler(info.ObjectAddress));
                   builder.AppendLine("ObjectCount=" + NullHandler(info.ObjectCount));
                   builder.AppendLine("ObjectTypeNumber=" + NullHandler(info.ObjectTypeNumber));
                   builder.AppendLine("PagedPollUsage=" + NullHandler(info.PagedPoolUsage));
                   builder.AppendLine("PointerCount=" + NullHandler(info.PointerCount));
                   builder.AppendLine("Type=" + NullHandler(info.Type));
                   
                   builder.AppendLine("");

               }
               Console.WriteLine(builder.ToString());


           }
           catch (Exception e)
           {
               builder.AppendLine(e.Message);
               Console.WriteLine("Info Get:" + e.Message);

           }
           return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
       }

       byte[] FillHeapDetail(int Pid)
       {
           StringBuilder builder = new StringBuilder();

           try
           {


               processInfos pfs = new processInfos();
               Dictionary<string, heapInfos> hnd = Heap.EnumerateHeapsByProcessId(Pid);

               foreach (heapInfos info in hnd.Values)
               {
                   builder.AppendLine("[Heap]");
                   builder.AppendLine("BaseAddress=" + NullHandler(info.BaseAddress));
                   builder.AppendLine("BlockCount=" + NullHandler(info.BlockCount));
                   builder.AppendLine("Flags=" + NullHandler(info.Flags));
                   builder.AppendLine("Granulraity=" + NullHandler(info.Granularity));
                   builder.AppendLine("MemAllocated=" + NullHandler(info.MemAllocated));
                   builder.AppendLine("MemCommited=" + NullHandler(info.MemCommitted));
                   builder.AppendLine("tagCount=" + NullHandler(info.TagCount));
                   builder.AppendLine("Tags=" + NullHandler(info.Tags));
                   
                   builder.AppendLine("");

               }
               Console.WriteLine(builder.ToString());


           }
           catch (Exception e)
           {
               builder.AppendLine(e.Message);
               Console.WriteLine("Info Get:" + e.Message);

           }
           return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
       }

       byte[] FillTokenDetail(int Pid)
       {
           StringBuilder builder = new StringBuilder();

           try
           {


               processInfos pfs = new processInfos();
               Native.Api.NativeStructs.PrivilegeInfo[] tkn = Token.GetPrivilegesListByProcessId(Pid);

               foreach (Native.Api.NativeStructs.PrivilegeInfo info in tkn)
               {
                   builder.AppendLine("[Privilages]");
                   builder.AppendLine("Name=" + NullHandler(info.Name));
                   builder.AppendLine("Privilage ID=" + NullHandler(info.pLuid));
                   builder.AppendLine("Status=" + NullHandler(info.Status));
                   
                   builder.AppendLine("");

               }
               Console.WriteLine(builder.ToString());


           }
           catch (Exception e)
           {
               builder.AppendLine(e.Message);
               Console.WriteLine("Info Get:" + e.Message);

           }
           return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
       } 
       
     byte[] FillEnvDetail(int Pid)
       {
           StringBuilder builder = new StringBuilder();

           try
           {


               processInfos pfs = new processInfos();
               builder.AppendLine("Pending");
               Console.WriteLine(builder.ToString());


           }
           catch (Exception e)
           {
               builder.AppendLine(e.Message);
               Console.WriteLine("Info Get:" + e.Message);

           }
           return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
       } 


       byte[] FillModuleDetail(int Pid)
       {
           StringBuilder builder = new StringBuilder();
           try
           {
              
              
               Console.WriteLine("fOR pROCESS " + Pid) ;
               
               if(!ProcessExists(Pid) && IsWin64(Pid))
                   return System.Text.Encoding.UTF8.GetBytes("Process Expired......");
               System.Diagnostics.Process prs=System.Diagnostics.Process.GetProcessById(Pid);
               System.Diagnostics.ProcessModuleCollection module= prs.Modules;
              
                
               if ( module!=null)
               {
                   foreach(SD.ProcessModule mod in module){
                    builder.AppendLine("[ " +NullHandler(mod.FileName)+ " ]");
                    builder.AppendLine("BaseAddres="+NullHandler(mod.BaseAddress));
                    builder.AppendLine("EntryPoint=" + NullHandler(mod.EntryPointAddress));
                    builder.AppendLine("ModuleMemorySize=" + NullHandler(mod.ModuleMemorySize));                    
                    builder.AppendLine("FileVersionInfo=" + NullHandler(mod.FileVersionInfo));
                    builder.AppendLine("[FileInfo]");
                    builder.AppendLine(NullHandler(mod.FileName));
                    builder.AppendLine("");
                      
                   }
                  
               }

              Console.WriteLine("Get "+builder.ToString());

           }
           catch (Exception e)
           {
               builder.AppendLine(e.Message);
               Console.WriteLine("Info Get:" + e.Message);
           }
           return System.Text.Encoding.UTF8.GetBytes(builder.ToString());

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
              builder.AppendLine(e.Message);
              Console.WriteLine("Info Get:"+e.Message); 
             
          }
          return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
       }

       byte[] FillMemoryDetail(int Pid)
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
               builder.AppendLine(e.Message);
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
               builder.AppendLine(e.Message);
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
               builder.AppendLine(e.Message);
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
                
                
                //Console.WriteLine("{0},{1},{2},{3}", filename,Node.CurrentNodeDir, Node.CurrentNodeFile,Node.isFile); 
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
                                    file = FillMemoryDetail(pid);
                                    break;
                                case "Network.inf":
                                    file = FillNetworkkDetail(pid);
                                    break;
                                case "Module.inf":
                                    file = FillModuleDetail(pid);
                                    break;
                                case "Thread.inf":
                                    file = FillThreadDetail(pid);
                                    break;
                                case "Token.inf":
                                    file = FillTokenDetail(pid);
                                    break;
                                case "Handle.inf":
                                    file = FillHandleDetail(pid);
                                    break;
                                case "Window.inf":
                                    file = FillWindowDetail(pid);
                                    break;
                                case "MemRegion.inf":
                                    file = FillMemRegionDetail(pid);
                                    break;
                                case "IO Counters.inf":
                                    file = FillIODetail(pid);
                                    break;
                                case "Heap.inf":
                                    file = FillHeapDetail(pid);
                                    break;                                    
                                default:
                                    file = FillEnvDetail(pid);
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
                                case "SystemInfo.inf":
                                file = FillSysDetail();
                                break;
                                case "Jobs.inf":
                                file = FillJobDetail();
                                break;
                               case "Services.inf":
                                file = FillServiceDetail();
                                break;                              
                               default:
                                file=FillEnvDetail(0);
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
