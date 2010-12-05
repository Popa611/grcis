﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;

namespace Scene3D
{
  public class Support
  {
    public static Matrix4 LookAt ( Vector3 camera, Vector3 center, Vector3 up )
    {
      Vector3 z = center - camera;
      z.Normalize();
      Vector3 x = Vector3.Cross( up, z );
      Vector3 y = Vector3.Cross(  z, x );
      x.Normalize();
      y.Normalize();
      z = Vector3.Multiply( z, -1.0f );
      Matrix4 tmp = new Matrix4( new Vector4( x ),
                                 new Vector4( y ),
                                 new Vector4( z ),
                                 Vector4.UnitW );
      tmp.Transpose();

      Matrix4 transl = Matrix4.CreateTranslation( Vector3.Multiply( camera, -1.0f ) );
      return Matrix4.Mult( transl, tmp );
    }

    public static Matrix4 SetViewport ( int x0, int y0, int width, int height )
    {
      Matrix4 tr = Matrix4.CreateTranslation( 1.0f, 1.0f, 1.0f );
      tr = Matrix4.Mult( tr, Matrix4.Scale( 0.5f * width, -0.5f * height, 0.5f ) );
      return Matrix4.Mult( tr, Matrix4.CreateTranslation( x0, y0 + height, 0.0f ) );
    }
  }

  /// <summary>
  /// Wireframe rendering of a 3D scene.
  /// </summary>
  public class Wireframe
  {
    #region Instance data

    /// <summary>
    /// Use perspective projection?
    /// </summary>
    public bool Perspective { get; set; }

    /// <summary>
    /// View vector: azimuth angle in degrees.
    /// </summary>
    public double Azimuth { get; set; }

    /// <summary>
    /// View vector: elevation angle in degrees.
    /// </summary>
    public double Elevation { get; set; }

    /// <summary>
    /// Camera distance (for perspective camera only).
    /// </summary>
    public double Distance { get; set; }

    /// <summary>
    /// Viewing volume (ortho: horizontal size, perspective: horizontal view angle in degrees).
    /// </summary>
    public double ViewVolume { get; set; }

    /// <summary>
    /// Draw normal vectors?
    /// </summary>
    public bool DrawNormals { get; set; }

    #endregion

    #region Construction

    /// <summary>
    /// Sets up default viewing parameters.
    /// </summary>
    public Wireframe ()
    {
      Perspective = false;
      Azimuth     = 30.0;
      Elevation   = 20.0;
      Distance    = 10.0;
      ViewVolume  = 60.0;
      DrawNormals = false;
    }

    #endregion

    #region Rendering API

    public void Render ( Bitmap output, SceneBrep scene )
    {
      if ( output == null ||
           scene  == null ) return;

      // center of the object = point to look at:
      double cx = 0.0;
      double cy = 0.0;
      double cz = 0.0;
      float minx = float.MaxValue;
      float miny = float.MaxValue;
      float minz = float.MaxValue;
      float maxx = float.MinValue;
      float maxy = float.MinValue;
      float maxz = float.MinValue;
      int n = scene.Vertices;
      int i;

      for ( i = 0; i < n; i++ )
      {
        Vector3 vi = scene.GetVertex( i );
        cx += vi.X;
        cy += vi.Y;
        cz += vi.Z;
        if ( vi.X < minx ) minx = vi.X;
        if ( vi.Y < miny ) miny = vi.Y;
        if ( vi.Z < minz ) minz = vi.Z;
        if ( vi.X > maxx ) maxx = vi.X;
        if ( vi.Y > maxy ) maxy = vi.Y;
        if ( vi.Z > maxz ) maxz = vi.Z;
      }
      Vector3 center = new Vector3( (float)(cx / n),
                                    (float)(cy / n),
                                    (float)(cz / n) );
      float diameter = (float)Math.Sqrt( (maxx - minx) * (maxx - minx) +
                                         (maxy - miny) * (maxy - miny) +
                                         (maxz - minz) * (maxz - minz) );

      // and the rest of projection matrix goes here:
      int width    = output.Width;
      int height   = output.Height;
      float aspect = width / (float)height;
      double az    = Azimuth / 180.0 * Math.PI;
      double el    = Elevation / 180.0 * Math.PI;

      Vector3 eye   = new Vector3( (float)(center.X + Distance * Math.Sin( az ) * Math.Cos( el )),
                                   (float)(center.Y + Distance * Math.Cos( az ) * Math.Cos( el )),
                                   (float)(center.Z + Distance * Math.Sin( el )) );
      Matrix4 modelView = Matrix4.LookAt( eye, center, Vector3.UnitY );
                          /* Support.LookAt( eye, center, Vector3.UnitZ ); */
      Matrix4 proj;

      if ( Perspective )
      {
        float vv = (float)(ViewVolume / 180.0 * Math.PI);
        proj = Matrix4.CreatePerspectiveFieldOfView( vv, aspect, 1.0f, 500.0f );
      }
      else
      {
        float vHalf = (float)(ViewVolume * 0.5);
        proj = Matrix4.CreateOrthographicOffCenter( -vHalf, vHalf,
                                                    -vHalf / aspect, vHalf / aspect,
                                                    1.0f, 50.0f );
      }

      Matrix4 compound = Matrix4.Mult( modelView, proj );
      Matrix4 viewport = Support.SetViewport( 0, 0, width, height );
      compound = Matrix4.Mult( compound, viewport );

      // wireframe rendering:
      Graphics gr = Graphics.FromImage( output );
      Pen pen = new Pen( Color.FromArgb( 255, 255, 80 ), 1.0f );
      n = scene.Triangles;
      for ( i = 0; i < n; i++ )
      {
        Vector3 A, B, C;
        scene.GetTriangleVertices( i, out A, out B, out C );
        A = Vector3.Transform( A, compound );
        B = Vector3.Transform( B, compound );
        C = Vector3.Transform( C, compound );
        gr.DrawLine( pen, A.X, A.Y, B.X, B.Y );
        gr.DrawLine( pen, B.X, B.Y, C.X, C.Y );
        gr.DrawLine( pen, C.X, C.Y, A.X, A.Y );
      }

      if ( DrawNormals && scene.Normals > 0 )
      {
        pen = new Pen( Color.FromArgb( 255, 80, 80 ), 1.0f );
        n = scene.Vertices;
        for ( i = 0; i < n; i++ )
        {
          Vector3 V, N;
          V = scene.GetVertex( i );
          N = scene.GetNormal( i );
          N.Normalize();
          N *= diameter * 0.03f;
          N += V;
          V = Vector3.Transform( V, compound );
          N = Vector3.Transform( N, compound );
          gr.DrawLine( pen, V.X, V.Y, N.X, N.Y );
        }
      }
    }

    #endregion

  }
}