﻿using System;
using System.Drawing;
using System.Windows.Forms;

namespace _080warping
{
  public partial class Form1 : Form
  {
    protected Image inputImage = null;
    protected Image outputImage = null;

    public Form1 ()
    {
      InitializeComponent();
      // !!! TODO: custom form resize
      // !!! TODO: 
      String[] tok = "$Rev$".Split( ' ' );
      Text += " (rev: " + tok[ 1 ] + ')';
    }

    private void buttonOpen_Click ( object sender, EventArgs e )
    {
      OpenFileDialog ofd = new OpenFileDialog();

      ofd.Title = "Open Image File";
      ofd.Filter = "Bitmap Files|*.bmp" +
          "|Gif Files|*.gif" +
          "|JPEG Files|*.jpg" +
          "|PNG Files|*.png" +
          "|TIFF Files|*.tif" +
          "|All image types|*.bmp;*.gif;*.jpg;*.png;*.tif";

      ofd.FilterIndex = 6;
      ofd.FileName = "";
      if ( ofd.ShowDialog() != DialogResult.OK )
        return;

      Image inp = Image.FromFile( ofd.FileName );
      inputImage = new Bitmap( inp );
      outputImage = new Bitmap( inp );
      inp.Dispose();

      recompute();
    }

    private void recompute ()
    {
      if ( inputImage == null ||
           outputImage == null ) return;

      pictureSource.SetPicture( (Bitmap)inputImage );
      pictureTarget.SetPicture( (Bitmap)outputImage );
    }

    private void buttonSave_Click ( object sender, EventArgs e )
    {
      if ( inputImage == null ||
           outputImage != null ) return;

      SaveFileDialog sfd = new SaveFileDialog();
      sfd.Title = "Save PNG file";
      sfd.Filter = "PNG Files|*.png";
      sfd.AddExtension = true;
      sfd.FileName = "";
      if ( sfd.ShowDialog() != DialogResult.OK )
        return;

      pictureTarget.GetPicture().Save( sfd.FileName, System.Drawing.Imaging.ImageFormat.Png );
    }

    private void numericParam_ValueChanged ( object sender, EventArgs e )
    {
      recompute();
    }
  }
}
