using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace EscapeTimer {
 public sealed class AudioDevice {
  public int Id; public string Name;
  public override string ToString() { return Name; }
 }
 public static class WaveNative {
  [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] public struct Caps {
   public ushort Mid,Pid; public uint Version;
   [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)] public string Name;
   public uint Formats; public ushort Channels,Reserved; public uint Support;
  }
  [StructLayout(LayoutKind.Sequential,Pack=2)] public struct Format {
   public ushort Tag,Channels; public uint Rate,Average; public ushort Block,Bits,Size;
  }
  [StructLayout(LayoutKind.Sequential)] public struct Header {
   public IntPtr Data; public uint Length,Recorded; public UIntPtr User;
   public uint Flags,Loops; public IntPtr Next; public UIntPtr Reserved;
  }
  [DllImport("winmm.dll")] public static extern uint waveOutGetNumDevs();
  [DllImport("winmm.dll",CharSet=CharSet.Unicode)] public static extern uint waveOutGetDevCapsW(UIntPtr id,out Caps caps,uint size);
  [DllImport("winmm.dll")] public static extern uint waveOutOpen(out IntPtr h,uint id,ref Format f,IntPtr cb,IntPtr instance,uint flags);
  [DllImport("winmm.dll")] public static extern uint waveOutGetID(IntPtr h,out uint id);
  [DllImport("winmm.dll")] public static extern uint waveOutPrepareHeader(IntPtr h,IntPtr head,uint size);
  [DllImport("winmm.dll")] public static extern uint waveOutWrite(IntPtr h,IntPtr head,uint size);
  [DllImport("winmm.dll")] public static extern uint waveOutUnprepareHeader(IntPtr h,IntPtr head,uint size);
  [DllImport("winmm.dll")] public static extern uint waveOutPause(IntPtr h);
  [DllImport("winmm.dll")] public static extern uint waveOutRestart(IntPtr h);
  [DllImport("winmm.dll")] public static extern uint waveOutReset(IntPtr h);
  [DllImport("winmm.dll")] public static extern uint waveOutClose(IntPtr h);
  public static Format Pcm { get { return new Format{Tag=1,Channels=1,Rate=44100,Average=88200,Block=2,Bits=16,Size=0}; } }
  public static List<AudioDevice> Devices() {
   List<AudioDevice> result=new List<AudioDevice>{new AudioDevice{Id=-2,Name="Sem áudio nesta saída"},new AudioDevice{Id=-1,Name="Saída padrão do Windows"}};
   for(uint i=0;i<waveOutGetNumDevs();i++) {
    Caps caps;
    if(waveOutGetDevCapsW(new UIntPtr(i),out caps,(uint)Marshal.SizeOf(typeof(Caps)))==0)
     result.Add(new AudioDevice{Id=(int)i,Name=(i+1)+" · "+caps.Name});
   }
   return result;
  }
 }
 public sealed class WaveTrack {
  private IntPtr handle,data,header;
  private bool prepared;
  private static uint HeaderSize=(uint)Marshal.SizeOf(typeof(WaveNative.Header));
  public uint ActualId;
  private static void Check(uint result,string operation) { if(result!=0) throw new InvalidOperationException(operation+" (código "+result+")"); }
  public WaveTrack(int device,byte[] pcm) {
   try {
    WaveNative.Format f=WaveNative.Pcm;
    Check(WaveNative.waveOutOpen(out handle,unchecked((uint)device),ref f,IntPtr.Zero,IntPtr.Zero,0),"Saída de som indisponível");
    Check(WaveNative.waveOutGetID(handle,out ActualId),"Identificação da saída");
    Check(WaveNative.waveOutPause(handle),"Preparação do som");
    data=Marshal.AllocHGlobal(pcm.Length); Marshal.Copy(pcm,0,data,pcm.Length);
    header=Marshal.AllocHGlobal((int)HeaderSize);
    Marshal.StructureToPtr(new WaveNative.Header{Data=data,Length=(uint)pcm.Length},header,false);
    Check(WaveNative.waveOutPrepareHeader(handle,header,HeaderSize),"Preparação do áudio"); prepared=true;
   } catch { Release(true); throw; }
  }
  public void Queue() { Check(WaveNative.waveOutWrite(handle,header,HeaderSize),"Envio do áudio"); }
  public void Start() { Check(WaveNative.waveOutRestart(handle),"Reprodução do áudio"); }
  public bool Done {
   get { if(header==IntPtr.Zero) return true; return (((WaveNative.Header)Marshal.PtrToStructure(header,typeof(WaveNative.Header))).Flags&1)!=0; }
  }
  public bool Release(bool stop) {
   if(handle!=IntPtr.Zero) {
    if(stop) WaveNative.waveOutReset(handle);
    if(prepared && WaveNative.waveOutUnprepareHeader(handle,header,HeaderSize)!=0) return false;
    prepared=false;
    if(WaveNative.waveOutClose(handle)!=0) return false;
    handle=IntPtr.Zero;
   }
   if(header!=IntPtr.Zero) { Marshal.FreeHGlobal(header); header=IntPtr.Zero; }
   if(data!=IntPtr.Zero) { Marshal.FreeHGlobal(data); data=IntPtr.Zero; }
   return true;
  }
 }
 public sealed class MultiAudio : IDisposable {
  private List<WaveTrack> tracks=new List<WaveTrack>();
  private List<WaveTrack> retired=new List<WaveTrack>();
  public string LastError="";
  public void Poll() {
   foreach(WaveTrack track in tracks.ToArray()) if(track.Done && track.Release(false)) tracks.Remove(track);
   foreach(WaveTrack track in retired.ToArray()) if(track.Release(true)) retired.Remove(track);
  }
  public void Stop() {
   foreach(WaveTrack track in tracks) if(!track.Release(true)) retired.Add(track);
   tracks.Clear(); Poll();
  }
  public static int[] DistinctTargets(IEnumerable<int> ids) { return ids.Where(i=>i>=-1).Distinct().ToArray(); }
  public static byte[] Pcm(string id,int volume) {
   byte[] wav=AudioPlayer.Scale(Assets.Read(id+".wav"),volume);
   for(int pos=12;pos+8<=wav.Length;) {
    int size=BitConverter.ToInt32(wav,pos+4);
    if(Encoding.ASCII.GetString(wav,pos,4)=="data") { byte[] result=new byte[size]; Buffer.BlockCopy(wav,pos+8,result,0,size); return result; }
    pos+=8+size+size%2;
   }
   throw new InvalidDataException("Sem dados no áudio.");
  }
  public int Play(string id,int volume,IEnumerable<int> outputs) {
   Stop(); LastError="";
   if(string.IsNullOrEmpty(id)||volume<=0) return 0;
   byte[] pcm=Pcm(id,volume);
   HashSet<uint> actual=new HashSet<uint>(); List<string> errors=new List<string>();
   foreach(int output in DistinctTargets(outputs)) {
    WaveTrack track=null;
    try {
     track=new WaveTrack(output,pcm);
     if(!actual.Add(track.ActualId)) { if(!track.Release(true)) retired.Add(track); continue; }
     tracks.Add(track);
    } catch(Exception e) { errors.Add(e.Message); }
   }
   foreach(WaveTrack track in tracks.ToArray()) {
    try { track.Queue(); } catch(Exception e) { errors.Add(e.Message); if(!track.Release(true)) retired.Add(track); tracks.Remove(track); }
   }
   foreach(WaveTrack track in tracks.ToArray()) {
    try { track.Start(); } catch(Exception e) { errors.Add(e.Message); if(!track.Release(true)) retired.Add(track); tracks.Remove(track); }
   }
   LastError=string.Join("; ",errors.Distinct()); return tracks.Count;
  }
  public void Dispose() { Stop(); }
 }
}
