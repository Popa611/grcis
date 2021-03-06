﻿using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Utilities;

namespace Rendering
{
  using ScriptContext = Dictionary<string, object>;

  /// <summary>
  /// Delegate used for GUI messaging.
  /// </summary>
  public delegate void StringDelegate (string msg);

  /// <summary>
  /// CSscripting support functions.
  /// </summary>
  public class Scripts
  {
    /// <summary>
    /// Reads additional scenes defined in a command-line arguments.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="repo">Existing scene repository to be modified (sceneName -&gt; sceneDelegate | scriptFileName).</param>
    /// <returns>How many scenes were found.</returns>
    public static int ReadFromConfig (string[] args, ScriptContext repo)
    {
      // <sceneFile.cs>
      // -scene <sceneFile>
      // -mask <sceneFile-mask>
      // -dir <directory>
      // more options to add? (super-sampling factor, output image file-name, output resolution, rendering flags, ..)

      int count = 0;
      for (int i = 0; i < args.Length; i++)
      {
        if (string.IsNullOrEmpty(args[i]))
          continue;

        string fileName = null;   // file-name or file-mask
        string dir = null;        // directory

        if (args[i][0] != '-')
        {
          if (File.Exists(args[i]))
            fileName = Path.GetFullPath(args[i]);
        }
        else
        {
          string opt = args[i].Substring(1);
          if (opt == "nodefault")
            repo.Clear();
          else if (opt == "scene" && i + 1 < args.Length)
          {
            if (File.Exists(args[++i]))
              fileName = Path.GetFullPath(args[i]);
          }
          else if (opt == "dir" && i + 1 < args.Length)
          {
            if (Directory.Exists(args[++i]))
            {
              dir = Path.GetFullPath(args[i]);
              fileName = "*.cs";
            }
          }
          else if (opt == "mask" && i + 1 < args.Length)
          {
            dir = Path.GetFullPath(args[++i]);
            fileName = Path.GetFileName(dir);
            dir = Path.GetDirectoryName(dir);
          }

          // Here new commands will be handled..
          // else if (opt == 'xxx' ..
        }

        if (!string.IsNullOrEmpty(dir))
        {
          if (!string.IsNullOrEmpty(fileName))
          {
            // valid dir & file-mask:
            try
            {
              string[] search = Directory.GetFiles(dir, fileName);
              foreach (string fn in search)
              {
                string path = Path.GetFullPath(fn);
                if (File.Exists(path))
                {
                  string key = Path.GetFileName(path);
                  if (key.EndsWith(".cs"))
                    key = key.Substring(0, key.Length - 3);

                  repo["* " + key] = path;
                  count++;
                }
              }
            }
            catch (IOException)
            {
              Console.WriteLine($"Warning: I/O error in dir/mask command: '{dir}'/'{fileName}'");
            }
            catch (UnauthorizedAccessException)
            {
              Console.WriteLine($"Warning: access error in dir/mask command: '{dir}'/'{fileName}'");
            }
          }
        }
        else if (!string.IsNullOrEmpty(fileName))
        {
          // single scene file:
          try
          {
            string path = Path.GetFullPath(fileName);
            if (File.Exists(path))
            {
              string key = Path.GetFileName(path);
              if (key.EndsWith(".cs"))
                key = key.Substring(0, key.Length - 3);

              repo["* " + key] = path;
              count++;
            }
          }
          catch (IOException)
          {
            Console.WriteLine($"Warning: I/O error in scene command: '{fileName}'");
          }
          catch (UnauthorizedAccessException)
          {
            Console.WriteLine($"Warning: access error in scene command: '{fileName}'");
          }
        }
      }

      return count;
    }

    public class Globals
    {
      /// <summary>
      /// Scene name (not used yet, might be useful).
      /// </summary>
      public string sceneName;

      /// <summary>
      /// Scene object to be filled.
      /// </summary>
      public IRayScene scene;

      /// <summary>
      /// Optional text parameter (usually from form's 'Params:' field).
      /// </summary>
      public string param;

      /// <summary>
      /// Parameter map for passing values in/out of the script.
      /// </summary>
      public ScriptContext context;
    }

    protected static int count = 0;

    /// <summary>
    /// Initializes the RT-script context before each individual call of 'SceneFromObject'.
    /// </summary>
    /// <param name="ctx">Pre-allocated context map.</param>
    /// <param name="sc">optional default scene object.</param>
    /// <param name="superSampling">Optional super-sampling coefficient.</param>
    /// <param name="minTime">Optional animation start time.</param>
    /// <param name="maxTime">optional animation finish time.</param>
    public static void ContextInit (
      in ScriptContext ctx,
      in DefaultRayScene sc = null,
      in int width = 640,
      in int height = 480,
      in int superSampling = 0,
      in double minTime = 0.0,
      in double maxTime = 10.0,
      in double fps = 25.0)
    {
      Debug.Assert(ctx != null);

      // Scene.
      if (sc != null)
        ctx[PropertyName.CTX_SCENE] = sc;
      else
        if (!ctx.ContainsKey(PropertyName.CTX_SCENE) ||
            ctx[PropertyName.CTX_SCENE] == null)
          ctx[PropertyName.CTX_SCENE] = new DefaultRayScene();

      ctx.Remove(PropertyName.CTX_ALGORITHM);
      ctx.Remove(PropertyName.CTX_SYNTHESIZER);

      // Resolution.
      ctx[PropertyName.CTX_WIDTH]  = width;
      ctx[PropertyName.CTX_HEIGHT] = height;

      // SuperSampling.
      ctx[PropertyName.CTX_SUPERSAMPLING] = superSampling;

      // Start.
      ctx[PropertyName.CTX_START_ANIM] = minTime;

      // End.
      ctx[PropertyName.CTX_END_ANIM] = maxTime;

      // End.
      ctx[PropertyName.CTX_FPS] = fps;
    }

    /// <summary>
    /// Retrieves standarda data frome the context after calling 'SceneFromObject'.
    /// </summary>
    /// <param name="imf">IImageFunction implementation if specified in the script.</param>
    /// <param name="rend">IRenderer if specified in the script.</param>
    /// <param name="tooltip">Tool-tip string if defined.</param>
    /// <param name="minTime">Animation start time if defined.</param>
    /// <param name="maxTime">Animation finish time if defined.</param>
    /// <param name="fps">Animation fps (frames per second).</param>
    /// <returns></returns>
    public static DefaultRayScene ContextMining (
      in ScriptContext ctx,
      out IImageFunction imf,
      out IRenderer rend,
      out string tooltip,
      ref int width,
      ref int height,
      ref int superSampling,
      ref double minTime,
      ref double maxTime,
      ref double fps)
    {
      Debug.Assert(ctx != null);

      imf     = null;
      rend    = null;
      tooltip = "";

      // Scene.
      if (!ctx.TryGetValue(PropertyName.CTX_SCENE, out object o) ||
          !(o is DefaultRayScene))
        return null;

      // IImageFunction.
      if (ctx.TryGetValue(PropertyName.CTX_ALGORITHM, out object o1) &&
          o1 is IImageFunction)
        imf = o1 as IImageFunction;

      // IRenderer.
      if (ctx.TryGetValue(PropertyName.CTX_SYNTHESIZER, out o1) &&
          o1 is IRenderer)
        rend = o1 as IRenderer;

      // Tooltip.
      if (ctx.TryGetValue(PropertyName.CTX_TOOLTIP, out o1) &&
          o1 is string)
        tooltip = o1 as string;

      // Resolution.
      Util.TryParse(ctx, PropertyName.CTX_WIDTH,  ref width);
      Util.TryParse(ctx, PropertyName.CTX_HEIGHT, ref height);

      // Super-sampling.
      Util.TryParse(ctx, PropertyName.CTX_SUPERSAMPLING, ref superSampling);

      // Start.
      Util.TryParse(ctx, PropertyName.CTX_START_ANIM, ref minTime);

      // End.
      Util.TryParse(ctx, PropertyName.CTX_END_ANIM, ref maxTime);

      // End.
      Util.TryParse(ctx, PropertyName.CTX_FPS, ref fps);

      return o as DefaultRayScene;
    }

    /// <summary>
    /// Compute a scene based on general description object 'definition' (one of delegate functions or CSscript file-name).
    /// </summary>
    /// <param name="name">Readable short scene name.</param>
    /// <param name="definition">Scene definition delegate function.</param>
    /// <param name="par">Text parameter (from form's text field..).</param>
    /// <param name="message">Message function</param>
    public static void SceneFromObject (
      in ScriptContext ctx,
      in string name,
      in object definition,
      in string par,
      in InitSceneDelegate defaultScene,
      in StringDelegate message = null)
    {
      Debug.Assert(ctx != null);

      DefaultRayScene sc;
      if (ctx.TryGetValue(PropertyName.CTX_SCENE, out object o) &&
          o is DefaultRayScene)
        sc = o as DefaultRayScene;
      else
        ctx[PropertyName.CTX_SCENE] = sc = new DefaultRayScene();

      string scriptFileName = definition as string;
      string scriptSource = null;

      if (!string.IsNullOrEmpty(scriptFileName) &&
          File.Exists(scriptFileName))
      {
        try
        {
          scriptSource = File.ReadAllText(scriptFileName);
        }
        catch (IOException)
        {
          Console.WriteLine($"Warning: I/O error in scene read: '{scriptFileName}'");
          scriptSource = null;
        }
        catch (UnauthorizedAccessException)
        {
          Console.WriteLine($"Warning: access error in scene read: '{scriptFileName}'");
          scriptSource = null;
        }

        if (!string.IsNullOrEmpty(scriptSource))
        {
          message?.Invoke($"Compiling and running scene script '{name}' ({++count})..");

          // interpret the CS-script defining the scene:
          var assemblyNames = Assembly.GetExecutingAssembly().GetReferencedAssemblies();

          List<Assembly> assemblies = new List<Assembly>();
          assemblies.Add(Assembly.GetExecutingAssembly());
          foreach (var assemblyName in assemblyNames)
            assemblies.Add(Assembly.Load(assemblyName));

          // Standard usings = imports.
          List<string> imports = new List<string>
          {
            "System",
            "System.Diagnostics",
            "System.Collections.Generic",
            "OpenTK",
            "MathSupport",
            "Rendering",
            "Utilities"
          };

          // Global variables for the script.
          Globals globals = new Globals
          {
            sceneName = name,
            scene     = sc,
            param     = par,
            context   = ctx
          };

          bool ok = true;
          try
          {
            var task = CSharpScript.RunAsync(
              scriptSource,
              globals: globals,
              options: ScriptOptions.Default.WithReferences(assemblies).AddImports(imports));

            Task.WaitAll(task);
          }
          catch (CompilationErrorException e)
          {
            MessageBox.Show($"Error compiling scene script: {e.Message}, using default scene", "CSscript Error");
            ok = false;
          }

          if (ok)
          {
            // Done.
            message?.Invoke($"Script '{name}' finished ok, rendering..");
            return;
          }
        }

        message?.Invoke("Using default scene..");
        defaultScene(sc);
        return;
      }

      // Script file doesn't exist => use delegate function instead.
      if (definition is InitSceneDelegate isd)
        isd(sc);
      else
        if (definition is InitSceneParamDelegate ispd)
          ispd(sc, par);

      message?.Invoke($"Rendering '{name}' ({++count})..");
      return;
    }
  }
}
