Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public static class LobbyAssetExport {
  public static void Key(Bitmap b) {
    var data = b.LockBits(new Rectangle(0,0,b.Width,b.Height),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
    byte[] pixels = new byte[Math.Abs(data.Stride)*b.Height];
    Marshal.Copy(data.Scan0,pixels,0,pixels.Length);
    for(int y=0;y<b.Height;y++) for(int x=0;x<b.Width;x++) {
      int i=y*data.Stride+x*4;
      int blue=pixels[i],green=pixels[i+1],red=pixels[i+2];
      if(red>150 && blue>150 && green<110 && red-green>80 && blue-green>80 && Math.Abs(red-blue)<60) {
        pixels[i]=255; pixels[i+1]=0; pixels[i+2]=255; pixels[i+3]=255;
      }
    }
    Marshal.Copy(pixels,0,data.Scan0,pixels.Length); b.UnlockBits(data);
  }
  public static void Export(string source,string destination,bool icons) {
    using(var src=new Bitmap(source)) using(var normalized=new Bitmap(src.Width,src.Height,PixelFormat.Format32bppArgb)) {
      using(var g=Graphics.FromImage(normalized)) g.DrawImageUnscaled(src,0,0);
      Key(normalized);
      if(!icons) { normalized.Save(destination,ImageFormat.Png); return; }
      using(var output=new Bitmap(1024,256,PixelFormat.Format32bppArgb)) {
        using(var g=Graphics.FromImage(output)) {
          g.Clear(Color.Magenta);
          g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
          g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
          float cellWidth=normalized.Width/4f;
          float scale=Math.Min(224f/cellWidth,256f/normalized.Height);
          float w=cellWidth*scale,h=normalized.Height*scale;
          for(int cell=0;cell<4;cell++) {
            var dest=new RectangleF(cell*256+(256-w)/2,(256-h)/2,w,h);
            var crop=new RectangleF(cell*cellWidth,0,cellWidth,normalized.Height);
            g.DrawImage(normalized,dest,crop,GraphicsUnit.Pixel);
          }
        }
        Key(output); output.Save(destination,ImageFormat.Png);
      }
    }
  }
}
'@
$sourceDir='C:\Users\user\.codex\generated_images\01a1edbd-3ecf-7120-8c68-f5d2f1d6790d'
$targetDir=Split-Path -Parent $PSScriptRoot
$assets=@(
  @('exec-407f960c-41ae-4573-b51b-04fa4aa5d15c.png','title_1159.png',$false),
  @('exec-e7756ebf-a714-428c-86ee-bf3bebc9a6c4.png','lobby_button.png',$false),
  @('exec-ab21f0ca-5fd7-4a97-a419-07dda85f2f5c.png','slot_card.png',$false),
  @('exec-5ca4bb18-10e1-4e8a-bbcc-e61557742eae.png','lobby_icons.png',$true)
)
foreach($asset in $assets) {
  $destination=Join-Path $targetDir $asset[1]
  if(Test-Path -LiteralPath $destination) { throw "Destination already exists: $destination" }
  [LobbyAssetExport]::Export((Join-Path $sourceDir $asset[0]),$destination,$asset[2])
  $bitmap=[System.Drawing.Bitmap]::new($destination)
  [pscustomobject]@{File=$asset[1];Width=$bitmap.Width;Height=$bitmap.Height;Corner=$bitmap.GetPixel(0,0).ToArgb().ToString('X8')}
  $bitmap.Dispose()
}
