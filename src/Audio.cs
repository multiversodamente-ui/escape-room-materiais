using System;
using System.IO;
using System.Media;
using System.Reflection;
using System.Text;

namespace EscapeTimer {
 public static class Assets {
  public static byte[] Read(string name) {
   using(Stream stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(name)) {
    if(stream==null) throw new FileNotFoundException("Recurso não encontrado: "+name);
    using(MemoryStream data=new MemoryStream()) { stream.CopyTo(data); return data.ToArray(); }
   }
  }
  public static string Text(string name) { return Encoding.UTF8.GetString(Read(name)); }
 }
 public sealed class AudioPlayer : IDisposable {
  private SoundPlayer player;
  private MemoryStream stream;
  public void Stop() {
   if(player!=null) { player.Stop(); player.Dispose(); player=null; }
   if(stream!=null) { stream.Dispose(); stream=null; }
  }
  public void Play(string id,int volume,bool loop) {
   Stop();
   if(string.IsNullOrEmpty(id) || volume<=0) return;
   byte[] data=Scale(Assets.Read(id+".wav"),volume);
   stream=new MemoryStream(data,false);
   player=new SoundPlayer(stream);
   player.Load();
   if(loop) player.PlayLooping(); else player.Play();
  }
  public static byte[] Scale(byte[] source,int volume) {
   if(source.Length<44 || Encoding.ASCII.GetString(source,0,4)!="RIFF" || Encoding.ASCII.GetString(source,8,4)!="WAVE") throw new InvalidDataException("Áudio WAV inválido.");
   byte[] data=(byte[])source.Clone();
   int pos=12, offset=-1, length=0; bool valid=false;
   while(pos+8<=data.Length) {
    string id=Encoding.ASCII.GetString(data,pos,4);
    int count=BitConverter.ToInt32(data,pos+4);
    if(count<0 || count>data.Length-pos-8) throw new InvalidDataException("Bloco WAV inválido.");
    if(id=="fmt " && count>=16) valid=BitConverter.ToInt16(data,pos+8)==1 && BitConverter.ToInt16(data,pos+22)==16;
    if(id=="data") { offset=pos+8; length=count; }
    pos+=8+count+(count%2);
   }
   if(!valid || offset<0 || length%2!=0) throw new InvalidDataException("É necessário áudio PCM de 16 bits.");
   double gain=Math.Max(0,Math.Min(100,volume))/100.0;
   for(int i=offset;i<offset+length;i+=2) {
    short sample=(short)Math.Round(BitConverter.ToInt16(data,i)*gain);
    data[i]=(byte)(sample&255); data[i+1]=(byte)((sample>>8)&255);
   }
   return data;
  }
  public void Dispose() { Stop(); }
 }
}
