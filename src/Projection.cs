using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using Screen=System.Windows.Forms.Screen;

namespace EscapeTimer {
 public sealed class Projection {
  public readonly Window Window;
  public readonly RoomTheme Theme;
  public DisplayFrame LastFrame;
  private readonly ThemeArt art;
  private Screen target;
  private bool full;
  public Action OpenControl;
  [DllImport("user32.dll",SetLastError=true)] static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int w,int h,uint flags);
  T Get<T>(string id) where T:class { return Window.FindName(id) as T; }
  public static Brush Brush(string color) { return (Brush)new BrushConverter().ConvertFromString(color); }
  public Projection(int room) {
   Theme=RoomTheme.Get(room);
   Window=(Window)XamlReader.Parse(Assets.Text("Projection.xaml"));
   Window.Title="Sala "+(room+1)+" · "+Theme.Name;
   art=new ThemeArt(Theme); Get<Grid>("ArtHost").Children.Add(art);
   Get<TextBlock>("RoomLabel").Text="SALA 0"+(room+1);
   Get<TextBlock>("RoomTitle").Text=Theme.Title;
   Get<TextBlock>("RoomTagline").Text=Theme.Tagline;
   Get<TextBlock>("Motto").Text=Theme.Motto;
   Get<TextBlock>("RoomLabel").Foreground=Brush(Theme.Accent);
   Get<System.Windows.Shapes.Rectangle>("AccentLine").Fill=Brush(Theme.Accent);
   Get<GradientStop>("ThemeStop").Color=(Color)ColorConverter.ConvertFromString(Theme.Dark);
   using(MemoryStream s=new MemoryStream(Assets.Read("logo.png"))) {
    BitmapImage b=new BitmapImage(); b.BeginInit(); b.CacheOption=BitmapCacheOption.OnLoad; b.StreamSource=s; b.EndInit(); b.Freeze(); Get<Image>("EventLogo").Source=b;
   }
   Window.PreviewKeyDown+=(s,e)=> {
    if(e.Key==Key.F2) { if(OpenControl!=null) OpenControl(); e.Handled=true; }
    else if(e.Key==Key.Escape) { ToWindow(); e.Handled=true; }
    else if(e.Key==Key.F11) { if(full) ToWindow(); else ToScreen(target??Screen.FromHandle(new WindowInteropHelper(Window).Handle)); e.Handled=true; }
   };
  }
  public void ShowPreview() { Window.Width=960; Window.Height=580; Window.Left=60+Theme.Id*65; Window.Top=60+Theme.Id*55; Window.Show(); }
  public void ToScreen(Screen screen) {
   target=screen; full=true;
   Window.WindowState=WindowState.Normal; Window.WindowStyle=WindowStyle.None; Window.ResizeMode=ResizeMode.NoResize;
   Window.Show();
   System.Drawing.Rectangle b=screen.Bounds;
   if(!SetWindowPos(new WindowInteropHelper(Window).Handle,IntPtr.Zero,b.Left,b.Top,b.Width,b.Height,0x0040|0x0010))
    throw new InvalidOperationException("Não foi possível posicionar a projeção na tela selecionada.");
  }
  private void ToWindow() { full=false; Window.WindowStyle=WindowStyle.SingleBorderWindow; Window.ResizeMode=ResizeMode.CanResize; Window.Width=1060; Window.Height=650; }
  public void Present(DisplayFrame frame,string endMessage) {
   LastFrame=frame;
   Get<TextBlock>("TimeDisplay").Text=frame.Time;
   string color=frame.Phase==Phase.Expired?"#FF826C":frame.Phase==Phase.Success?"#79E8B7":frame.Danger?"#FFCB86":Theme.Accent;
   Get<TextBlock>("TimeDisplay").Foreground=Brush(frame.Phase==Phase.Expired?color:"#F3F9FF");
   Get<TextBlock>("StatusText").Foreground=Brush(color);
   Get<Border>("Progress").Background=Brush(color); Get<Border>("Progress").Width=1020*frame.Fraction;
   Get<Border>("StatusBadge").Background=Brush(frame.Phase==Phase.Expired?"#492A2C":frame.Danger?"#443126":Theme.Dark);
   string status="AGUARDANDO INÍCIO",detail="Um relógio. Três salas. Uma missão.";
   if(frame.Phase==Phase.Running) { status=frame.Danger?"ALERTA · TEMPO CRÍTICO":"MISSÃO EM ANDAMENTO"; detail=frame.Danger?"O tempo está acabando. A próxima decisão importa.":"Cada descoberta aproxima a equipe da saída."; }
   if(frame.Phase==Phase.Paused) { status="CONTAGEM PAUSADA"; detail="O relógio aguarda. A missão continua em instantes."; }
   if(frame.Phase==Phase.Expired) { status="TEMPO ESGOTADO"; detail=endMessage; }
   if(frame.Phase==Phase.Success) { status="MISSÃO CUMPRIDA"; detail="Vocês transformaram conhecimento em solução."; }
   Get<TextBlock>("StatusText").Text=status; Get<TextBlock>("Detail").Text=detail;
   art.Advance(frame.AnimationTime,frame.Danger);
  }
 }
}
