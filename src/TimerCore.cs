using System;

namespace EscapeTimer {
 public enum Phase { Ready, Running, Paused, Expired, Success }
 public sealed class Countdown {
  public Phase Phase { get; private set; }
  public double Total { get; private set; }
  public double Remaining { get; private set; }
  private double deadline;
  public Countdown(double seconds) { Reset(seconds); }
  public void Reset(double seconds) {
   if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 1 || seconds > 59999) throw new ArgumentOutOfRangeException("seconds");
   Total=Remaining=seconds; Phase=Phase.Ready;
  }
  public void Start(double now) {
   if (Phase!=Phase.Ready && Phase!=Phase.Paused) return;
   deadline=now+Remaining; Phase=Phase.Running;
  }
  public bool Tick(double now) {
   if (Phase!=Phase.Running) return false;
   Remaining=Math.Max(0,deadline-now);
   if (Remaining>0) return false;
   Phase=Phase.Expired; return true;
  }
  public void Pause(double now) { Tick(now); if(Phase==Phase.Running) Phase=Phase.Paused; }
  public void Complete(double now) {
   Tick(now);
   if(Phase==Phase.Running || Phase==Phase.Paused) Phase=Phase.Success;
  }
  public int DisplaySeconds { get { return (int)Math.Ceiling(Remaining); } }
  public string Display { get { return Format(DisplaySeconds); } }
  public static string Format(int seconds) { return (seconds/60).ToString("00")+":"+(seconds%60).ToString("00"); }
 }
 public class Settings {
  public int Seconds=3600;
  public string Title="LABORATÓRIO SECRETO";
  public string EndMessage="Tempo esgotado. O laboratório entrou em colapso.";
  public int AlertIndex=1;
  public int FinalIndex=3;
  public int AlertSeconds=60;
  public int Volume=35;
  public void Normalize() {
   Seconds=Math.Max(1,Math.Min(59999,Seconds));
   AlertSeconds=Math.Max(0,Math.Min(9999,AlertSeconds));
   AlertIndex=Math.Max(0,Math.Min(2,AlertIndex)); FinalIndex=Math.Max(0,Math.Min(3,FinalIndex));
   Volume=Math.Max(0,Math.Min(100,Volume));
   if(string.IsNullOrWhiteSpace(Title)) Title="LABORATÓRIO SECRETO";
   if(Title.Length>65) Title=Title.Substring(0,65);
   if(string.IsNullOrWhiteSpace(EndMessage)) EndMessage="O tempo acabou. Aguardem o monitor.";
   if(EndMessage.Length>110) EndMessage=EndMessage.Substring(0,110);
  }
 }
}
