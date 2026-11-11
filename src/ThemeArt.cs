using System;
using System.Windows;
using System.Windows.Media;

namespace EscapeTimer {
 public sealed class RoomTheme {
  public int Id; public string Name,Title,Tagline,Motto,Accent,Dark;
  public static RoomTheme Get(int id) {
   if(id==0) return new RoomTheme{Id=0,Name="Crise Climática",Title="CRISE\nCLIMÁTICA",Tagline="O planeta precisa de equilíbrio.",Motto="OBSERVE. INVESTIGUE. TRANSFORME.",Accent="#65E5D0",Dark="#123A3D"};
   if(id==1) return new RoomTheme{Id=1,Name="Segurança Alimentar",Title="SEGURANÇA\nALIMENTAR",Tagline="O futuro começa com o que preservamos.",Motto="PROTEGER HOJE. ALIMENTAR O AMANHÃ.",Accent="#F3CA7C",Dark="#303B24"};
   return new RoomTheme{Id=2,Name="Energia do Futuro",Title="ENERGIA\nDO FUTURO",Tagline="A próxima transformação está em suas mãos.",Motto="TRANSFORME CONHECIMENTO EM ENERGIA.",Accent="#B5A7FF",Dark="#262950"};
  }
 }
 public sealed class ThemeArt : FrameworkElement {
  private readonly RoomTheme theme; private double time; private bool danger;
  public ThemeArt(RoomTheme t) { theme=t; Width=1920; Height=1080; IsHitTestVisible=false; }
  public void Advance(double seconds,bool alarm) { time=seconds; danger=alarm; InvalidateVisual(); }
  private Brush B(string hex,double opacity) { SolidColorBrush b=(SolidColorBrush)new BrushConverter().ConvertFromString(hex); b.Opacity=opacity; b.Freeze(); return b; }
  private Pen P(string color,double width,double alpha) { Pen p=new Pen(B(color,alpha),width); p.Freeze(); return p; }
  private void Line(DrawingContext d,double x,double y,double xx,double yy,string color,double width,double alpha) { d.DrawLine(P(color,width,alpha),new Point(x,y),new Point(xx,yy)); }
  private void Path(DrawingContext d,string path,string color,double width,double alpha) { d.DrawGeometry(null,P(color,width,alpha),Geometry.Parse(path)); }
  private void Leaf(DrawingContext d,double x,double y,double angle,double scale,string color) {
   d.PushTransform(new TranslateTransform(x,y)); d.PushTransform(new RotateTransform(angle)); d.PushTransform(new ScaleTransform(scale,scale));
   d.DrawGeometry(B(color,0.16),P(color,1.7,0.65),Geometry.Parse("M 0,0 C -42,-2 -55,-35 -56,-65 C -15,-62 7,-27 0,0 Z"));
   Line(d,0,0,-42,-49,color,1.5,0.7); d.Pop(); d.Pop(); d.Pop();
  }
  protected override void OnRender(DrawingContext d) {
   base.OnRender(d); string accent=theme.Accent;
   // Large quiet geometry keeps the clock clear while the right-hand scene carries the theme.
   for(int i=0;i<7;i++) d.DrawEllipse(null,P(accent,1,0.025),new Point(1558,550),210+i*63,210+i*63);
   for(int i=0;i<25;i++) {
    double x=1230+(i*137%610),y=260+(i*91%660),pulse=0.1+0.1*(1+Math.Sin(time*0.65+i))/2;
    d.DrawEllipse(B(accent,pulse),null,new Point(x,y),2,2);
   }
   if(theme.Id==0) Climate(d); else if(theme.Id==1) Food(d); else Energy(d);
   if(danger) d.DrawRoundedRectangle(null,P("#FFB265",2,0.3+Math.Sin(time*1.3)*0.08),new Rect(76,72,1768,936),26,26);
  }
  private void Climate(DrawingContext d) {
   string a=theme.Accent; double cx=1540,cy=540,r=208;
   d.DrawEllipse(B(a,0.025),P(a,2,0.48),new Point(cx,cy),r,r);
   d.DrawEllipse(null,P(a,1,0.16),new Point(cx,cy),125,r);
   d.DrawEllipse(null,P(a,1,0.13),new Point(cx,cy),49,r);
   for(int i=-2;i<=2;i++) d.DrawEllipse(null,P(a,1,0.12),new Point(cx,cy+i*52),Math.Sqrt(r*r-i*i*52*52),32);
   d.PushTransform(new TranslateTransform(cx,cy));
   d.DrawGeometry(B(a,0.20),P(a,1.5,0.7),Geometry.Parse("M -135,-104 L -84,-145 -33,-126 -25,-87 12,-66 -8,-20 -53,4 -38,48 -62,109 -103,70 -99,18 -144,-18 -157,-65 Z"));
   d.DrawGeometry(B(a,0.16),P(a,1.5,0.55),Geometry.Parse("M 50,-113 L 119,-100 158,-46 106,-9 120,48 75,102 44,64 61,9 22,-24 Z"));
   d.Pop();
   double move=Math.Sin(time*0.3)*14;
   Path(d,"M 1255,379 C 1400,318 1560,358 1795,292",a,2,0.25);
   Path(d,"M 1260,782 C 1440,708 1660,822 1810,736",a,2,0.25);
   d.DrawEllipse(B(a,0.8),null,new Point(1500+move,340),4,4);
   for(int i=0;i<6;i++) {
    double x=1292+i*78,top=860-(i*53%105);
    d.DrawRoundedRectangle(B("#0A222B",0.9),P(a,1,0.23),new Rect(x,top,52,945-top),3,3);
    for(int w=0;w<2;w++) for(int h=0;h<4;h++) if(top+18+h*24<927) d.DrawRectangle(B(a,0.15),null,new Rect(x+11+w*20,top+15+h*24,7,9));
   }
   for(int i=0;i<4;i++) { double x=1340+i*125; Line(d,x,940,x,852,a,3,0.55); Leaf(d,x,885,-20+Math.Sin(time*0.4)*3,0.7,a); Leaf(d,x,868,95,0.65,a); }
  }
  private void Food(DrawingContext d) {
   string a=theme.Accent;
   d.DrawEllipse(null,P(a,2,0.25),new Point(1550,566),230,230);
   for(int i=0;i<7;i++) Path(d,"M 1270,"+(835+i*20)+" Q 1530,"+(710+i*25)+" 1820,"+(820+i*21),a,1.2,0.18);
   for(int stem=0;stem<3;stem++) {
    double x=1420+stem*117,y=715-stem*12;
    d.PushTransform(new RotateTransform(Math.Sin(time*0.4+stem)*1.5,x,y));
    Line(d,x,y,x,y-260,a,3,0.7);
    for(int i=0;i<5;i++) { Leaf(d,x,y-60-i*36,-17,0.5,a); Leaf(d,x,y-78-i*36,97,0.5,a); }
    d.Pop();
   }
   for(int i=0;i<3;i++) {
    double x=1360+i*136,y=797;
    d.DrawRoundedRectangle(B("#172920",0.93),P(a,1.8,0.6),new Rect(x,y,118,107),9,9);
    Line(d,x+5,y+31,x+113,y+31,a,1,0.25); Line(d,x+5,y+70,x+113,y+70,a,1,0.25);
    Leaf(d,x+59,y+72,35,0.42,a);
   }
   d.DrawEllipse(B("#A9C98B",0.15),P("#A9C98B",1.5,0.6),new Point(1287,600),19,19);
   d.DrawEllipse(B(a,0.2),null,new Point(1811,589+Math.Sin(time)*3),6,6);
  }
  private void Energy(DrawingContext d) {
   string a=theme.Accent; double cx=1610,cy=500;
   d.DrawEllipse(null,P(a,1.5,0.22),new Point(cx,cy),205,205);
   Line(d,cx,cy,cx-18,811,a,7,0.6);
   d.PushTransform(new RotateTransform(time*8,cx,cy));
   for(int i=0;i<3;i++) {
    d.PushTransform(new RotateTransform(i*120,cx,cy));
    d.DrawGeometry(B(a,0.16),P(a,2,0.7),Geometry.Parse("M 1610,500 L 1601,435 1621,309 1637,316 1627,449 Z")); d.Pop();
   }
   d.Pop(); d.DrawEllipse(B(a,0.8),null,new Point(cx,cy),12,12);
   d.DrawRoundedRectangle(B("#1D2342",0.9),P(a,2,0.6),new Rect(1296,656,130,180),15,15);
   d.DrawRoundedRectangle(B(a,0.3),null,new Rect(1334,644,54,12),4,4);
   for(int i=0;i<3;i++) d.DrawRectangle(B(a,0.14+0.13*(1+Math.Sin(time*0.8-i))/2),null,new Rect(1316,684+i*46,90,31));
   d.DrawGeometry(B("#152742",0.9),P(a,2,0.55),Geometry.Parse("M 1492,812 L 1776,812 1820,916 1451,916 Z"));
   for(int i=1;i<5;i++) Line(d,1492+i*56,812,1451+i*73,916,a,1,0.32);
   for(int i=1;i<3;i++) Line(d,1492-i*14,812+i*35,1776+i*15,812+i*35,a,1,0.32);
   Path(d,"M 1428,747 L 1494,747 1494,707 1587,707",a,2,0.45);
   Path(d,"M 1362,838 L 1362,948 1674,948 1674,918",a,2,0.45);
   double p=(time*0.2)%1; d.DrawEllipse(B(a,0.9),null,new Point(1362+312*p,948),4.5,4.5);
  }
 }
}
