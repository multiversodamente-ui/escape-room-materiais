using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EscapeTimer {
 public sealed class ShowSettings {
  public readonly Window Window;
  public ShowConfig Result;
  private TextBox minutes,seconds,ending;
  private ComboBox pace,final;
  private CheckBox automatic;
  private Slider volume;
  private TextBlock total,error;
  private Dictionary<string,CheckBox> enabled=new Dictionary<string,CheckBox>();
  private static Brush B(string color) { return Projection.Brush(color); }
  private static TextBlock Label(string text,int size=14) { return new TextBlock{Text=text,FontSize=size,Foreground=B("#CFDBEC"),Margin=new Thickness(0,0,0,7),TextWrapping=TextWrapping.Wrap}; }
  private static TextBox Box(string text,int width=0) { TextBox b=new TextBox{Text=text,FontSize=18,Padding=new Thickness(10,7,10,7),Background=B("#20324D"),Foreground=Brushes.White,BorderBrush=B("#49617F"),Margin=new Thickness(0,0,12,0)}; if(width>0)b.Width=width; return b; }
  private static Button Button(string text) { return new Button{Content=text,Padding=new Thickness(12,8,12,8),Margin=new Thickness(8,0,0,0),FontSize=14,Background=B("#DCE8F8"),Foreground=B("#172842")}; }
  public ShowSettings(ShowConfig current,Action<string,int> preview,Action stop) {
   Window=new Window{Title="Tempo e sons automáticos",Width=760,Height=900,MinWidth=650,MinHeight=500,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=B("#101B2D"),Foreground=Brushes.White,FontFamily=new FontFamily("Segoe UI")};
   StackPanel root=new StackPanel{Margin=new Thickness(28)};
   ScrollViewer scroll=new ScrollViewer{Content=root,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Background=B("#101B2D")}; Window.Content=scroll;
   root.Children.Add(Label("Prepare o tempo e a atmosfera",28));
   root.Children.Add(Label("As escolhas valem para as três salas. Aplicar inicia uma nova programação.",13));
   StackPanel duration=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,15,0,18)};
   StackPanel mins=new StackPanel(); mins.Children.Add(Label("Minutos")); minutes=Box((current.Seconds/60).ToString(),130); minutes.MaxLength=3; mins.Children.Add(minutes);
   StackPanel secs=new StackPanel(); secs.Children.Add(Label("Segundos")); seconds=Box((current.Seconds%60).ToString("00"),130); seconds.MaxLength=2; secs.Children.Add(seconds);
   duration.Children.Add(mins); duration.Children.Add(secs);
   foreach(int preset in new[]{30,45,60}) { int value=preset; Button p=Button(preset+" min"); p.VerticalAlignment=VerticalAlignment.Bottom; p.Click+=(s,e)=>{minutes.Text=value.ToString();seconds.Text="00";}; duration.Children.Add(p); }
   root.Children.Add(duration);
   automatic=new CheckBox{Content="Reproduzir efeitos automaticamente durante a contagem",IsChecked=current.Automatic,Foreground=Brushes.White,FontSize=16,Margin=new Thickness(0,0,0,15)}; root.Children.Add(automatic);
   root.Children.Add(Label("Ritmo dos efeitos"));
   pace=new ComboBox{ItemsSource=new[]{"Leve · a cada 5 minutos","Equilibrado · a cada 3 minutos","Intenso · a cada 90 segundos"},SelectedIndex=current.Pace,FontSize=16,Padding=new Thickness(8),Margin=new Thickness(0,0,0,10)}; root.Children.Add(pace);
   root.Children.Add(Label("No último minuto, o ritmo aumenta: efeitos aos 60, 40, 20 e 10 segundos restantes. Rodadas curtas são ajustadas para evitar sobreposição.",12));
   total=Label("",18); total.Foreground=B("#6BE2CD"); total.FontWeight=FontWeights.SemiBold; total.Margin=new Thickness(0,8,0,15); root.Children.Add(total);
   root.Children.Add(Label("Sons que entram no revezamento",17));
   Grid library=new Grid{Margin=new Thickness(0,0,0,14)}; library.ColumnDefinitions.Add(new ColumnDefinition()); library.ColumnDefinitions.Add(new ColumnDefinition());
   for(int row=0;row<4;row++) library.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
   for(int i=0;i<SoundLibrary.RotationIds.Length;i++) {
    string id=SoundLibrary.RotationIds[i]; Grid cell=new Grid{Margin=new Thickness(0,0,12,8)}; cell.ColumnDefinitions.Add(new ColumnDefinition()); cell.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(65)});
    CheckBox check=new CheckBox{Content=SoundLibrary.Name(id),IsChecked=current.Enabled.Contains(id),Foreground=Brushes.White,FontSize=13,VerticalAlignment=VerticalAlignment.Center}; enabled.Add(id,check); cell.Children.Add(check);
    Button hear=Button("Ouvir"); hear.FontSize=11; hear.Padding=new Thickness(7); Grid.SetColumn(hear,1); hear.Click+=(s,e)=>preview(id,(int)volume.Value); cell.Children.Add(hear);
    Grid.SetRow(cell,i/2); Grid.SetColumn(cell,i%2); library.Children.Add(cell);
   }
   root.Children.Add(library);
   root.Children.Add(Label("Som ao chegar a zero"));
   Grid finalRow=new Grid(); finalRow.ColumnDefinitions.Add(new ColumnDefinition()); finalRow.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(80)});
   final=new ComboBox{ItemsSource=SoundLibrary.AllIds.Select(SoundLibrary.Name).ToArray(),SelectedIndex=Array.IndexOf(SoundLibrary.AllIds,current.FinalSound),FontSize=15,Padding=new Thickness(8)}; finalRow.Children.Add(final);
   Button hearFinal=Button("Ouvir"); Grid.SetColumn(hearFinal,1); hearFinal.Click+=(s,e)=>preview(SoundLibrary.AllIds[final.SelectedIndex],(int)volume.Value); finalRow.Children.Add(hearFinal); root.Children.Add(finalRow);
   Grid soundRow=new Grid{Margin=new Thickness(0,16,0,14)}; soundRow.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(70)}); soundRow.ColumnDefinitions.Add(new ColumnDefinition()); soundRow.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(55)}); soundRow.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(120)});
   soundRow.Children.Add(Label("Volume")); volume=new Slider{Minimum=0,Maximum=100,Value=current.Volume,VerticalAlignment=VerticalAlignment.Center}; Grid.SetColumn(volume,1); soundRow.Children.Add(volume);
   TextBlock number=Label(current.Volume+"%"); Grid.SetColumn(number,2); number.VerticalAlignment=VerticalAlignment.Center; number.Margin=new Thickness(7,0,0,0); soundRow.Children.Add(number);
   Button halt=Button("Parar som"); halt.Click+=(s,e)=>stop(); Grid.SetColumn(halt,3); soundRow.Children.Add(halt); root.Children.Add(soundRow);
   volume.ValueChanged+=(s,e)=> { number.Text=((int)volume.Value)+"%"; stop(); };
   root.Children.Add(Label("Mensagem ao terminar")); ending=Box(current.EndMessage); ending.Margin=new Thickness(0); ending.MaxLength=110; root.Children.Add(ending);
   error=Label(""); error.Foreground=B("#FFBA81"); error.Margin=new Thickness(0,10,0,5); root.Children.Add(error);
   StackPanel actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,8,0,0)};
   Button cancel=Button("Cancelar"),apply=Button("Aplicar e reiniciar"); apply.Background=B("#5ED4BE"); apply.FontWeight=FontWeights.SemiBold;
   cancel.Click+=(s,e)=> { Window.DialogResult=false; }; apply.Click+=(s,e)=> { ShowConfig value=Read(); if(value!=null) { Result=value; Window.DialogResult=true; } };
   actions.Children.Add(cancel); actions.Children.Add(apply); root.Children.Add(actions);
   minutes.TextChanged+=(s,e)=>Count(); seconds.TextChanged+=(s,e)=>Count(); pace.SelectionChanged+=(s,e)=>Count(); final.SelectionChanged+=(s,e)=>Count(); automatic.Click+=(s,e)=>Count();
   foreach(CheckBox c in enabled.Values) c.Click+=(s,e)=>Count(); Window.Closed+=(s,e)=>stop(); Count();
  }
  private ShowConfig Read() {
   int m,s;
   if(!int.TryParse(minutes.Text,out m)||!int.TryParse(seconds.Text,out s)||m<0||m>999||s<0||s>59||m*60+s==0) { error.Text="Use 0 a 999 minutos e 0 a 59 segundos; duração mínima de 1 segundo."; return null; }
   string[] selected=enabled.Where(e=>e.Value.IsChecked==true).Select(e=>e.Key).ToArray();
   if(automatic.IsChecked==true && selected.Length==0) { error.Text="Escolha ao menos um efeito ou desative a reprodução automática."; return null; }
   error.Text="";
   return new ShowConfig{Seconds=m*60+s,Pace=Math.Max(0,pace.SelectedIndex),Enabled=selected,Automatic=automatic.IsChecked==true,FinalSound=SoundLibrary.AllIds[Math.Max(0,final.SelectedIndex)],Volume=(int)volume.Value,EndMessage=ending.Text};
  }
  private void Count() {
   ShowConfig c=Read();
   if(c==null) { total.Text="Confira a duração e os sons selecionados."; return; }
   SoundPlan p=new SoundPlan(c,0); int endingCount=string.IsNullOrEmpty(c.FinalSound)?0:1;
   total.Text=p.Cues.Count+" disparos: "+(p.Cues.Count-endingCount)+" durante a contagem + "+endingCount+" no final.";
  }
 }
}
