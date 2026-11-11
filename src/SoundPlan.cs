using System;
using System.Collections.Generic;
using System.Linq;

namespace EscapeTimer {
 public class ShowConfig : Settings {
  public ShowConfig() { EndMessage="O tempo acabou. Aguardem as orientações do monitor."; }
  public int Pace=1;
  public bool Automatic=true;
  public string[] Enabled=SoundLibrary.RotationIds;
  public string FinalSound="colapso";
  public void Validate() {
   Normalize(); Pace=Math.Max(0,Math.Min(2,Pace));
   Enabled=(Enabled??new string[0]).Where(id=>SoundLibrary.RotationIds.Contains(id)).Distinct().ToArray();
   if(!SoundLibrary.AllIds.Contains(FinalSound)) FinalSound="colapso";
  }
 }
 public static class SoundLibrary {
  public static readonly string[] RotationIds={"alarme","sirene","estrondo","explosao","impacto","queda","energia","faiscas"};
  public static readonly string[] AllIds=RotationIds.Concat(new[]{"colapso",""}).ToArray();
  public static string Name(string id) {
   switch(id) {
    case "alarme": return "Alarme de perigo"; case "sirene": return "Sirene eletrônica";
    case "estrondo": return "Estrondo de destruição"; case "explosao": return "Explosão";
    case "impacto": return "Impacto grave"; case "queda": return "Queda de frequência";
    case "energia": return "Descarga elétrica"; case "faiscas": return "Faíscas / curto-circuito";
    case "colapso": return "Explosão e colapso"; default:return "Sem som";
   }
  }
 }
 public sealed class ShuffleBag {
  private readonly string[] pool; private readonly Random rng; private List<string> bag=new List<string>(); private string last="";
  public ShuffleBag(IEnumerable<string> values,int seed) { pool=values.Distinct().ToArray(); rng=new Random(seed); }
  public string Next() {
   if(pool.Length==0) return "";
   if(bag.Count==0) {
    bag=pool.OrderBy(x=>rng.Next()).ToList();
    if(bag.Count>1 && bag[0]==last) { string v=bag[0]; bag[0]=bag[1]; bag[1]=v; }
   }
   string result=bag[0]; bag.RemoveAt(0); last=result; return result;
  }
 }
 public sealed class SoundCue {
  public int At; public string Id; public bool Final;
  public string Description { get { return Countdown.Format(At)+"  ·  "+SoundLibrary.Name(Id)+(Final?"  [FINAL]":""); } }
 }
 public sealed class SoundPlan {
  public readonly List<SoundCue> Cues; private int cursor;
  public int Passed { get { return cursor; } }
  public int RemainingCount { get { return Cues.Count-cursor; } }
  public SoundCue NextCue { get { return cursor<Cues.Count?Cues[cursor]:null; } }
  public SoundPlan(ShowConfig cfg,int seed) {
   Cues=new List<SoundCue>(); SortedSet<int> times=new SortedSet<int>();
   int interval=new[]{300,180,90}[cfg.Pace];
   if(cfg.Automatic && cfg.Enabled.Length>0 && cfg.Seconds>=10) {
    times.Add(0);
    for(int at=interval;at<cfg.Seconds-60;at+=interval) times.Add(at);
    foreach(int remaining in new[]{60,40,20,10}) if(cfg.Seconds-remaining>=0) times.Add(cfg.Seconds-remaining);
   }
   ShuffleBag bag=new ShuffleBag(cfg.Enabled,seed); int previous=-10;
   foreach(int at in times) {
    if(at-previous<10 || cfg.Seconds-at<10) continue;
    Cues.Add(new SoundCue{At=at,Id=bag.Next()}); previous=at;
   }
   if(!string.IsNullOrEmpty(cfg.FinalSound)) Cues.Add(new SoundCue{At=cfg.Seconds,Id=cfg.FinalSound,Final=true});
  }
  public SoundCue Advance(double elapsed) {
   SoundCue latest=null;
   while(cursor<Cues.Count && Cues[cursor].At<=elapsed+0.000001) latest=Cues[cursor++];
   // Drop old cues after a delayed UI update; never fire a backlog of sounds.
   return latest!=null && (latest.Final || elapsed-latest.At<=2)?latest:null;
  }
 }
 public sealed class DisplayFrame {
  public readonly string Time;
  public readonly double Fraction;
  public readonly double AnimationTime;
  public readonly Phase Phase;
  public readonly bool Danger;
  public DisplayFrame(Countdown timer,double animationTime) {
   Time=timer.Display; Fraction=timer.Remaining/timer.Total; Phase=timer.Phase;
   Danger=Phase==Phase.Running && timer.Remaining<=60; AnimationTime=animationTime;
  }
 }
}
