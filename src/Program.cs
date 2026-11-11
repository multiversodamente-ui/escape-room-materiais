using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using Screen=System.Windows.Forms.Screen;

namespace EscapeTimer {
 public static class Program {
  [STAThread] public static int Main(string[] args) {
   try {
    if(args.Length>1 && args[0]=="--self-test") return Tests.Run(args[1]);
    if(args.Length>1 && args[0]=="--hardware-info") return Tests.Hardware(args[1]);
    if(args.Length>1 && args[0]=="--audio-silent-test") return Tests.SilentAudio(args[1]);
    Application app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
    app.DispatcherUnhandledException+=(s,e)=> { MessageBox.Show(e.Exception.Message,"Cronômetro",MessageBoxButton.OK,MessageBoxImage.Warning); e.Handled=true; };
    Controller c=new Controller();
    if(args.Length>1 && args[0]=="--render-preview") { c.RenderPreviews(args[1]); return 0; }
    app.Run(c.Window); return 0;
   } catch(Exception e) {
    if(args.Length>0) { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"erro-verificacao.txt"),e.ToString()); return 1; }
    MessageBox.Show("Não foi possível abrir o aplicativo.\n"+e.Message,"Escape Room"); return 1;
   }
  }
 }
 public sealed class MonitorChoice {
  public Screen Screen; public string Label;
  public override string ToString() { return Label; }
 }
 public sealed class Controller {
  public Window Window;
  private ShowConfig cfg;
  private Countdown timer;
  private SoundPlan plan;
  private readonly Stopwatch clock=Stopwatch.StartNew();
  private readonly DispatcherTimer pulse=new DispatcherTimer();
  private readonly MultiAudio audio=new MultiAudio();
  private readonly List<Projection> views=new List<Projection>();
  private readonly ComboBox[] monitorBoxes=new ComboBox[3],audioBoxes=new ComboBox[3];
  private bool muted,closing,replacingViews,awake;
  private int played,missed;
  private string lastSound="",notice="";
  private readonly string profile=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"configuracao-tres-salas.xml");
  [DllImport("kernel32.dll")] static extern uint SetThreadExecutionState(uint flags);
  private T Get<T>(string id) where T:class { return (T)Window.FindName(id); }
  private double Now { get { return clock.Elapsed.TotalSeconds; } }
  private static Brush B(string color) { return Projection.Brush(color); }
  private void SetText(string id,string value) { Get<TextBlock>(id).Text=value; }
  public Controller() {
   cfg=Load(); cfg.Validate(); timer=new Countdown(cfg.Seconds); RebuildPlan();
   Window=(Window)XamlReader.Parse(Assets.Text("Dashboard.xaml"));
   BuildRooms(); RefreshDevices();
   Get<Button>("StartButton").Click+=(s,e)=>Toggle();
   Get<Button>("ResetButton").Click+=(s,e)=>Reset();
   Get<Button>("SuccessButton").Click+=(s,e)=>Complete();
   Get<Button>("ConfigButton").Click+=(s,e)=>Configure();
   Get<Button>("MuteButton").Click+=(s,e)=> { muted=!muted; if(muted) audio.Stop(); Update(); };
   Get<Button>("RefreshButton").Click+=(s,e)=>RefreshDevices();
   Get<Button>("PreviewButton").Click+=(s,e)=>OpenViews(false);
   Get<Button>("ProjectButton").Click+=(s,e)=>OpenViews(true);
   Get<Button>("CloseViewsButton").Click+=(s,e)=>CloseViews();
   Get<Button>("PlanButton").Click+=(s,e)=>ShowText("Programação dos sons",PlanText());
   Get<Button>("CreditsButton").Click+=(s,e)=>ShowText("Áudios e créditos",Assets.Text("CREDITOS.txt"));
   Window.PreviewKeyDown+=(s,e)=> {
    if(e.IsRepeat || Keyboard.FocusedElement is ComboBox || Keyboard.FocusedElement is TextBox) return;
    if(e.Key==Key.Space) { Toggle(); e.Handled=true; }
    if(e.Key==Key.Escape && views.Count>0) { Window.Hide(); views[0].Window.Activate(); e.Handled=true; }
   };
   Window.Closing+=(s,e)=> {
    if(timer.Phase==Phase.Running && MessageBox.Show(Window,"Encerrar a contagem e as três projeções?","Encerrar",MessageBoxButton.YesNo)!=MessageBoxResult.Yes) { e.Cancel=true; return; }
    closing=true; pulse.Stop(); audio.Dispose(); KeepAwake(false); CloseViews();
   };
   Window.Closed+=(s,e)=>Application.Current.Shutdown();
   pulse.Interval=TimeSpan.FromMilliseconds(50); pulse.Tick+=(s,e)=>Tick(); pulse.Start(); Update();
  }
  private ShowConfig Load() {
   try { if(File.Exists(profile)) using(FileStream s=File.OpenRead(profile)) return (ShowConfig)new XmlSerializer(typeof(ShowConfig)).Deserialize(s); } catch { }
   return new ShowConfig();
  }
  private void Save() { try { using(FileStream s=File.Create(profile)) new XmlSerializer(typeof(ShowConfig)).Serialize(s,cfg); } catch { notice="Configurações válidas nesta sessão; não foi possível gravar nesta pasta."; } }
  private void KeepAwake(bool value) { if(awake!=value) { SetThreadExecutionState(value?0x80000003u:0x80000000u); awake=value; } }
  private void RebuildPlan() { plan=new SoundPlan(cfg,Environment.TickCount); played=missed=0; lastSound=""; }
  private void BuildRooms() {
   StackPanel host=Get<StackPanel>("RoomsPanel");
   for(int i=0;i<3;i++) {
    int room=i; RoomTheme theme=RoomTheme.Get(i);
    Border panel=new Border{Background=B("#142033"),BorderBrush=B(theme.Accent),BorderThickness=new Thickness(3,0,0,0),CornerRadius=new CornerRadius(9),Padding=new Thickness(17,12,17,12),Margin=new Thickness(0,0,0,9)};
    Grid g=new Grid(); foreach(double width in new[]{260d,1d,1d,116d}) g.ColumnDefinitions.Add(new ColumnDefinition{Width=width==1?new GridLength(1,GridUnitType.Star):new GridLength(width)});
    StackPanel title=new StackPanel{VerticalAlignment=VerticalAlignment.Center};
    title.Children.Add(new TextBlock{Text="SALA 0"+(i+1),FontSize=10,Foreground=B(theme.Accent),FontWeight=FontWeights.Bold});
    title.Children.Add(new TextBlock{Text=theme.Name,FontSize=18,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,4,8,0)}); g.Children.Add(title);
    StackPanel displays=new StackPanel{Margin=new Thickness(6,0,14,0)}; Grid.SetColumn(displays,1);
    displays.Children.Add(new TextBlock{Text="TELA / PROJETOR",FontSize=10,Foreground=B("#8FA7C8"),Margin=new Thickness(0,0,0,5)});
    monitorBoxes[i]=new ComboBox{IsSynchronizedWithCurrentItem=false}; displays.Children.Add(monitorBoxes[i]); g.Children.Add(displays);
    StackPanel sounds=new StackPanel{Margin=new Thickness(0,0,14,0)}; Grid.SetColumn(sounds,2);
    sounds.Children.Add(new TextBlock{Text="SAÍDA DE ÁUDIO",FontSize=10,Foreground=B("#8FA7C8"),Margin=new Thickness(0,0,0,5)});
    audioBoxes[i]=new ComboBox{IsSynchronizedWithCurrentItem=false}; sounds.Children.Add(audioBoxes[i]); g.Children.Add(sounds);
    audioBoxes[i].SelectionChanged+=(s,e)=>Update();
    Button test=new Button{Content="Testar som",FontSize=12,Padding=new Thickness(10,9,10,9),Margin=new Thickness(0),VerticalAlignment=VerticalAlignment.Bottom}; Grid.SetColumn(test,3);
    test.Click+=(s,e)=> {
     if(timer.Phase==Phase.Running) { notice="Pause para testar saídas de áudio."; Update(); return; }
     AudioDevice selected=audioBoxes[room].SelectedItem as AudioDevice;
     int count=audio.Play("alarme",cfg.Volume,selected==null?new int[0]:new[]{selected.Id});
     notice=count>0?"Teste enviado à saída da Sala "+(room+1)+". Confirme o som nessa sala.":"Selecione uma saída ativa para a Sala "+(room+1)+". "+audio.LastError; Update();
    };
    g.Children.Add(test); panel.Child=g; host.Children.Add(panel);
   }
  }
  private void RefreshDevices() {
   if(timer.Phase==Phase.Running) { notice="Pause para atualizar conexões."; Update(); return; }
   audio.Stop();
   Screen[] screens=Screen.AllScreens.OrderBy(s=>s.Bounds.X).ThenBy(s=>s.Bounds.Y).ToArray();
   List<AudioDevice> devices=WaveNative.Devices();
   for(int i=0;i<3;i++) {
    string oldName=(monitorBoxes[i].SelectedItem as MonitorChoice)==null?null:((MonitorChoice)monitorBoxes[i].SelectedItem).Label;
    List<MonitorChoice> options=new List<MonitorChoice>{new MonitorChoice{Label="Selecione uma tela"}};
    for(int n=0;n<screens.Length;n++) options.Add(new MonitorChoice{Screen=screens[n],Label="Tela "+(n+1)+" · "+screens[n].Bounds.Width+"×"+screens[n].Bounds.Height+(screens[n].Primary?" · principal":"")});
    monitorBoxes[i].ItemsSource=options;
    monitorBoxes[i].SelectedItem=options.FirstOrDefault(m=>m.Label==oldName)??options[(i<screens.Length)?i+1:0];
    string oldAudio=(audioBoxes[i].SelectedItem as AudioDevice)==null?null:((AudioDevice)audioBoxes[i].SelectedItem).Name;
    audioBoxes[i].ItemsSource=devices.ToList();
    audioBoxes[i].SelectedItem=((List<AudioDevice>)audioBoxes[i].ItemsSource).FirstOrDefault(d=>d.Name==oldAudio)??((List<AudioDevice>)audioBoxes[i].ItemsSource)[1];
   }
   SetText("DeviceStatus",screens.Length+" tela(s) detectada(s) e "+(devices.Count-2)+" saída(s) de áudio. Use o modo Estender do Windows. F2 abre o controle; ESC no controle retorna às projeções.");
   notice="Selecione o HDMI ou a caixa de cada sala e use Testar som. Saídas repetidas recebem uma única reprodução."; Update();
  }
  private int[] Outputs() { return audioBoxes.Where(b=>b!=null).Select(b=>b.SelectedItem as AudioDevice).Where(d=>d!=null).Select(d=>d.Id).ToArray(); }
  private void OpenControl() { Window.Show(); Window.WindowState=WindowState.Normal; Window.Activate(); }
  private void CloseViews() {
   replacingViews=true; foreach(Projection p in views.ToArray()) p.Window.Close(); views.Clear(); replacingViews=false;
   if(!closing) OpenControl();
  }
  private void OpenViews(bool project) {
   MonitorChoice[] choices=monitorBoxes.Select(b=>b.SelectedItem as MonitorChoice).ToArray();
   if(project && (choices.Any(c=>c==null||c.Screen==null)||choices.Select(c=>c.Screen.DeviceName).Distinct().Count()!=3)) {
    notice="Selecione três telas diferentes. Com menos telas conectadas, use Prévia dos três temas."; Update(); return;
   }
   if(project && choices.Any(c=>!Screen.AllScreens.Any(s=>s.DeviceName==c.Screen.DeviceName))) {
    notice="Uma tela foi desconectada. Atualize as conexões antes de projetar."; Update(); return;
   }
   CloseViews();
   try {
    for(int i=0;i<3;i++) {
     Projection p=new Projection(i); p.OpenControl=OpenControl; views.Add(p);
     p.Window.Closed+=(s,e)=> { views.Remove(p); if(!replacingViews&&!closing&&views.Count==0) OpenControl(); };
     if(project) p.ToScreen(choices[i].Screen); else p.ShowPreview();
    }
    Update(); if(project) { Window.Hide(); views[0].Window.Activate(); }
   } catch(Exception e) { notice=e.Message; CloseViews(); Update(); }
  }
  private void Tick() {
   timer.Tick(Now); audio.Poll(); KeepAwake(timer.Phase==Phase.Running);
   if(timer.Phase==Phase.Running || timer.Phase==Phase.Expired) ConsumeSound();
   Update();
  }
  private void ConsumeSound() {
   int previous=plan.Passed;
   SoundCue cue=plan.Advance(timer.Total-timer.Remaining);
   int advanced=plan.Passed-previous;
   if(cue!=null && !muted && cfg.Volume>0) {
    int targets=audio.Play(cue.Id,cfg.Volume,Outputs());
    if(targets>0) { played++; lastSound=SoundLibrary.Name(cue.Id); missed+=Math.Max(0,advanced-1); }
    else { missed+=advanced; notice="Nenhuma saída reproduziu o efeito. "+audio.LastError; }
    if(!string.IsNullOrEmpty(audio.LastError)) notice=audio.LastError;
   } else missed+=advanced;
  }
  private void Toggle() {
   if(timer.Phase==Phase.Running) {
    timer.Pause(Now); audio.Stop(); if(timer.Phase==Phase.Expired) ConsumeSound();
   } else if(timer.Phase==Phase.Ready || timer.Phase==Phase.Paused) timer.Start(Now);
   Tick();
  }
  private void Reset() {
   if((timer.Phase==Phase.Running||timer.Phase==Phase.Paused)&&MessageBox.Show(Window,"Reiniciar o tempo nas três salas?","Nova rodada",MessageBoxButton.YesNo)!=MessageBoxResult.Yes) return;
   audio.Stop(); timer.Reset(cfg.Seconds); KeepAwake(false); RebuildPlan(); notice="Nova rodada preparada."; Update();
  }
  private void Complete() {
   if(timer.Phase!=Phase.Running&&timer.Phase!=Phase.Paused) return;
   timer.Complete(Now); audio.Stop(); KeepAwake(false);
   if(timer.Phase==Phase.Expired) ConsumeSound(); Update();
  }
  private void Update() {
   if(Window==null) return;
   SetText("MainTime",timer.Display);
   string status=timer.Phase==Phase.Ready?"Pronto para começar":timer.Phase==Phase.Running?"Contagem sincronizada nas três salas":timer.Phase==Phase.Paused?"Pausado nas três salas":timer.Phase==Phase.Expired?"Tempo esgotado nas três salas":"Missão concluída";
   SetText("StageText",status);
   Get<Button>("StartButton").Content=timer.Phase==Phase.Running?"PAUSAR AS 3 SALAS":timer.Phase==Phase.Paused?"CONTINUAR NAS 3 SALAS":"INICIAR NAS 3 SALAS";
   Get<Button>("StartButton").IsEnabled=timer.Phase!=Phase.Success&&timer.Phase!=Phase.Expired;
   Get<Button>("ConfigButton").IsEnabled=timer.Phase!=Phase.Running;
   Get<Button>("RefreshButton").IsEnabled=timer.Phase!=Phase.Running;
   Get<Button>("SuccessButton").IsEnabled=timer.Phase==Phase.Running||timer.Phase==Phase.Paused;
   Get<Button>("MuteButton").Content=muted?"Reativar som":"Silenciar";
   SetText("PlanCount",plan.Cues.Count+" disparos programados · "+played+" reproduzidos");
   SoundCue next=plan.NextCue;
   string nextText=next==null?"Programação concluída.":"Próximo: "+SoundLibrary.Name(next.Id)+" · quando restar "+Countdown.Format(cfg.Seconds-next.At)+".";
   if(timer.Phase==Phase.Success) nextText="Missão concluída: os próximos efeitos foram cancelados.";
   SetText("NextSound",nextText+(missed>0?"  "+missed+" omitido(s) por silêncio, atraso ou saída indisponível.":""));
   SetText("AudioStatus",notice);
   DisplayFrame snapshot=new DisplayFrame(timer,Now);
   foreach(Projection p in views.ToArray()) p.Present(snapshot,cfg.EndMessage);
  }
  private string PlanText() {
   return "PROGRAMAÇÃO DA RODADA\r\n\r\nDuração: "+Countdown.Format(cfg.Seconds)+"\r\nTotal: "+plan.Cues.Count+" disparos.\r\n\r\nTempos abaixo são decorridos desde o início.\r\nA lista alterna os efeitos selecionados sem repetir o mesmo em seguida (se houver mais de um).\r\n\r\n"+string.Join("\r\n",plan.Cues.Select(c=>c.Description))+"\r\n\r\nPausar interrompe o som e preserva o relógio. Ao retomar, efeitos antigos não são repetidos. Silenciar não acumula efeitos para depois.";
  }
  private void ShowText(string title,string text) {
   Window d=new Window{Title=title,Owner=Window,Width=760,Height=640,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=B("#101B2D")};
   d.Content=new TextBox{Text=text,IsReadOnly=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Background=B("#101B2D"),Foreground=B("#EDF4FF"),Margin=new Thickness(24),BorderThickness=new Thickness(0),FontSize=15}; d.ShowDialog();
  }
  private void Configure() {
   if(timer.Phase==Phase.Running) return;
   audio.Stop(); ShowSettings dialog=new ShowSettings(cfg,(id,v)=> { audio.Play(id,v,Outputs()); },()=>audio.Stop());
   dialog.Window.Owner=Window;
   if(dialog.Window.ShowDialog()==true) { cfg=dialog.Result; cfg.Validate(); timer.Reset(cfg.Seconds); RebuildPlan(); Save(); notice="Tempo e programação aplicados às três salas."; }
   audio.Stop(); Update();
  }
  public static void Render(FrameworkElement element,string file,int w,int h) {
   element.Measure(new Size(w,h)); element.Arrange(new Rect(0,0,w,h)); element.UpdateLayout();
   RenderTargetBitmap bitmap=new RenderTargetBitmap(w,h,96,96,PixelFormats.Pbgra32); bitmap.Render(element);
   PngBitmapEncoder encoder=new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using(FileStream f=File.Create(file)) encoder.Save(f);
  }
  public void RenderPreviews(string folder) {
   Directory.CreateDirectory(folder); pulse.Stop();
   timer.Reset(3600); timer.Start(0); timer.Tick(188); DisplayFrame frame=new DisplayFrame(timer,8);
   for(int i=0;i<3;i++) { Projection p=new Projection(i); p.Present(frame,cfg.EndMessage); Render((FrameworkElement)p.Window.Content,Path.Combine(folder,"Sala_0"+(i+1)+".png"),1920,1080); }
   timer.Reset(45); timer.Start(0); Projection warning=new Projection(0); warning.Present(new DisplayFrame(timer,7),cfg.EndMessage);
   Render((FrameworkElement)warning.Window.Content,Path.Combine(folder,"Alerta.png"),1920,1080);
   timer.Tick(45); warning.Present(new DisplayFrame(timer,7),cfg.EndMessage); Render((FrameworkElement)warning.Window.Content,Path.Combine(folder,"Encerramento.png"),1920,1080);
   timer.Reset(cfg.Seconds); Update(); Render((FrameworkElement)Window.Content,Path.Combine(folder,"Controle.png"),1240,860);
   ShowSettings settings=new ShowSettings(cfg,(id,v)=>{},()=>{}); Render((FrameworkElement)settings.Window.Content,Path.Combine(folder,"Configuracoes.png"),760,870);
   audio.Dispose(); KeepAwake(false);
  }
 }
}
