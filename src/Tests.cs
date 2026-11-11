using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;

namespace EscapeTimer {
 public static class Tests {
  private static List<string> lines=new List<string>();
  private static void Check(bool value,string text) { if(!value) throw new Exception(text); lines.Add("OK - "+text); }
  public static int Run(string path) {
   try {
    Countdown c=new Countdown(60); c.Start(10); c.Tick(10.5); Check(c.Display=="01:00","Exibição não perde segundos antes do tempo");
    c.Pause(11.25); double remain=c.Remaining; c.Tick(200); Check(c.Remaining==remain,"Pausa conserva a precisão");
    c.Start(300); c.Tick(350); Check(Math.Abs(c.Remaining-8.75)<0.00001,"Retomada correta");
    Check(c.Tick(358.75) && !c.Tick(500) && c.Remaining==0,"Fim único no instante exato");
    c.Reset(1);c.Start(0);c.Pause(1);Check(c.Phase==Phase.Expired,"Pausa no limite encerra a contagem");
    c.Reset(120);c.Start(0);c.Complete(40);Check(c.Phase==Phase.Success&&c.Display=="01:20","Conclusão congela o tempo restante");
    ShowConfig cfg=new ShowConfig(); SoundPlan p=new SoundPlan(cfg,10);
    Check(p.Cues.Count==25 && p.Cues.Count(x=>!x.Final)==24,"60 minutos equilibrado: 24 efeitos e 1 final");
    cfg.Seconds=1800; Check(new SoundPlan(cfg,10).Cues.Count==15,"30 minutos equilibrado: 15 disparos");
    cfg.Seconds=3600;cfg.Pace=0;Check(new SoundPlan(cfg,10).Cues.Count==17,"Ritmo leve: 17 disparos em 60 minutos");
    cfg.Pace=2;Check(new SoundPlan(cfg,10).Cues.Count==45,"Ritmo intenso: 45 disparos em 60 minutos");
    for(int duration=1;duration<=7200;duration+=7) {
     cfg.Seconds=duration; SoundPlan shortPlan=new SoundPlan(cfg,2);
     for(int i=1;i<shortPlan.Cues.Count;i++) if(shortPlan.Cues[i].At-shortPlan.Cues[i-1].At<10) throw new Exception("Sobreposição em "+duration);
     if(shortPlan.Cues.Any(x=>x.At>duration)) throw new Exception("Efeito depois do fim");
    }
    Check(true,"Mais de mil durações verificadas: intervalo mínimo de 10 s e nenhum efeito após o fim");
    cfg=new ShowConfig();p=new SoundPlan(cfg,12);
    Check(p.Advance(0)!=null && p.Advance(0)==null,"Primeiro efeito não se repete");
    Check(p.Advance(500)==null && p.Passed==3,"Efeitos atrasados são descartados sem rajada");
    Check(p.Advance(540.2)!=null && p.Advance(540.4)==null,"Próximo efeito toca uma vez após atraso");
    Check(p.Advance(9999).Final && p.Advance(9999)==null,"Final único, mesmo com grande atraso");
    ShuffleBag bag=new ShuffleBag(SoundLibrary.RotationIds,5); string last=""; HashSet<string> first=new HashSet<string>();
    for(int i=0;i<500;i++) { string id=bag.Next(); if(id==last) throw new Exception("Repetição imediata"); if(i<8) first.Add(id); last=id; }
    Check(first.Count==8,"Revezamento usa oito sons e evita repetições consecutivas");
    cfg.Automatic=false;Check(new SoundPlan(cfg,0).Cues.Count==1,"Automático desativado conserva somente o final");
    cfg.FinalSound="";Check(new SoundPlan(cfg,0).Cues.Count==0,"Todos os sons podem ser desativados");
    Check(MultiAudio.DistinctTargets(new[]{1,1,2,-2,3}).SequenceEqual(new[]{1,2,3}),"Saídas repetidas não multiplicam o mesmo som");
    Check(Marshal.SizeOf(typeof(WaveNative.Format))==18,"Estrutura PCM válida");
    Check(Marshal.SizeOf(typeof(WaveNative.Header))==(IntPtr.Size==8?48:32),"Buffers nativos compatíveis com a arquitetura");
    foreach(string id in SoundLibrary.AllIds.Where(s=>s!="")) {
     byte[] pcm=MultiAudio.Pcm(id,35),zero=MultiAudio.Pcm(id,0);
     Check(pcm.Length>0 && pcm.Length%2==0 && zero.All(b=>b==0),"Áudio e silêncio válidos: "+id);
    }
    Application app=new Application(); c.Reset(3600);c.Start(0);c.Tick(123.1); DisplayFrame frame=new DisplayFrame(c,10);
    Projection[] rooms={new Projection(0),new Projection(1),new Projection(2)};
    foreach(Projection room in rooms) room.Present(frame,"Fim");
    Check(rooms.All(r=>object.ReferenceEquals(r.LastFrame,frame)) && rooms.Select(r=>r.LastFrame.Time).Distinct().Count()==1,"As três salas recebem a mesma leitura do mesmo relógio");
    Check(rooms.Select(r=>r.Theme.Name).Distinct().Count()==3,"Três temas distintos carregados");
    Check(Assets.Text("CREDITOS.txt").Contains("Brian MacIntosh"),"Créditos dos novos efeitos incluídos");
    File.WriteAllLines(path,lines); return 0;
   } catch(Exception e) { lines.Add("FALHOU - "+e);File.WriteAllLines(path,lines);return 1; }
  }
  public static int SilentAudio(string path) {
   List<string> report=new List<string>(); List<WaveTrack> tracks=new List<WaveTrack>();
   try {
    byte[] silence=new byte[22050];
    foreach(AudioDevice d in WaveNative.Devices().Where(d=>d.Id>=0)) {
     tracks.Add(new WaveTrack(d.Id,silence));
     report.Add("Preparada com silêncio: "+d.Name);
    }
    foreach(WaveTrack track in tracks) track.Queue();
    foreach(WaveTrack track in tracks) track.Start();
    System.Diagnostics.Stopwatch watch=System.Diagnostics.Stopwatch.StartNew();
    while(tracks.Any(t=>!t.Done)&&watch.Elapsed.TotalSeconds<4) System.Threading.Thread.Sleep(10);
    if(tracks.Count==0) throw new Exception("Nenhuma saída de áudio disponível para verificar.");
    if(tracks.Any(t=>!t.Done)) throw new Exception("Uma saída não concluiu o buffer silencioso.");
    foreach(WaveTrack track in tracks) if(!track.Release(false)) throw new Exception("Uma saída não liberou o buffer.");
    report.Add("OK: envio simultâneo e conclusão de buffers silenciosos nas "+tracks.Count+" saídas detectadas. Nenhum efeito audível foi reproduzido.");
    File.WriteAllLines(path,report);return 0;
   } catch(Exception e) {
    foreach(WaveTrack track in tracks) track.Release(true);
    report.Add("FALHOU: "+e);File.WriteAllLines(path,report);return 1;
   }
  }
  public static int Hardware(string path) {
   List<string> result=new List<string>();
   foreach(System.Windows.Forms.Screen s in System.Windows.Forms.Screen.AllScreens) result.Add("TELA: "+s.DeviceName+" "+s.Bounds);
   foreach(AudioDevice d in WaveNative.Devices()) {
    if(d.Id < -1) continue;
    WaveNative.Format f=WaveNative.Pcm; IntPtr h;
    uint status=WaveNative.waveOutOpen(out h,unchecked((uint)d.Id),ref f,IntPtr.Zero,IntPtr.Zero,1);
    result.Add("ÁUDIO: "+d.Name+" | Consulta PCM: "+status);
   }
   result.Add("Consulta de capacidade, sem reprodução de som."); File.WriteAllLines(path,result);return 0;
  }
 }
}
