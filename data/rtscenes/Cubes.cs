//////////////////////////////////////////////////
// Preprocessing stage support.
bool preprocessing = false;

if (context != null)
{
  // context["ToolTip"] indicates whether the script is running for the first time (preprocessing) or for regular rendering.
  preprocessing = !context.ContainsKey(PropertyName.CTX_TOOLTIP);
  if (preprocessing)
  {
    context[PropertyName.CTX_TOOLTIP] = "n=<double> (index of refraction)\rmat={mirror|glass}}";

    // TODO: put scene preprocessing code here
    // Store results in any context[] object, sunsequent calls will find it there..

    return;
  }

  // Optional IImageFunction.
  context[PropertyName.CTX_ALGORITHM] = new RayTracing();
}

if (scene.BackgroundColor != null)
  return;    // scene can be shared!

//////////////////////////////////////////////////
// CSG scene.

CSGInnerNode root = new CSGInnerNode(SetOperation.Union);
root.SetAttribute(PropertyName.REFLECTANCE_MODEL, new PhongModel());
root.SetAttribute(PropertyName.MATERIAL, new PhongMaterial(new double[] {1.0, 0.6, 0.1}, 0.1, 0.8, 0.2, 16));
scene.Intersectable = root;

// Background color.
scene.BackgroundColor = new double[] {0.0, 0.05, 0.07};

// Camera.
scene.Camera = new StaticCamera(new Vector3d(0.7, 3.0, -10.0),
                                new Vector3d(0.0, -0.3, 1.0),
                                50.0);

// Light sources.
scene.Sources = new System.Collections.Generic.LinkedList<ILightSource>();
scene.Sources.Add(new AmbientLightSource(0.8));
scene.Sources.Add(new PointLightSource(new Vector3d(-5.0, 3.0, -3.0), 1.0));

// --- NODE DEFINITIONS ----------------------------------------------------

// Params dictionary.
Dictionary<string, string> p = Util.ParseKeyValueList(param);

// n = <index-of-refraction>
double n = 1.6;
Util.TryParse(p, "n", ref n);

// mat = {mirror|glass|diffuse}
PhongMaterial pm = new PhongMaterial(new double[] {1.0, 0.6, 0.1}, 0.1, 0.8, 0.2, 16);
string mat;
if (p.TryGetValue("mat", out mat))
  switch (mat)
  {
    case "mirror":
      pm = new PhongMaterial(new double[] {1.0, 1.0, 0.8}, 0.0, 0.1, 0.9, 128);
      break;

    case "glass":
      pm = new PhongMaterial(new double[] {0.0, 0.2, 0.1}, 0.05, 0.05, 0.1, 128);
      pm.n = n;
      pm.Kt = 0.9;
      break;
  }

// Base plane.
Plane pl = new Plane();
pl.SetAttribute(PropertyName.COLOR, new double[] {0.6, 0.0, 0.0});
pl.SetAttribute(PropertyName.TEXTURE, new CheckerTexture(0.5, 0.5, new double[] {1.0, 1.0, 1.0}));
root.InsertChild(pl, Matrix4d.RotateX(-MathHelper.PiOver2) * Matrix4d.CreateTranslation(0.0, -1.0, 0.0));

// Cubes.
Cube c;

// The front row.
c = new Cube();
root.InsertChild(c, Matrix4d.RotateY(0.6) * Matrix4d.CreateTranslation(-3.5, -0.8, 0.0));
c.SetAttribute(PropertyName.MATERIAL, pm);
c = new Cube();
root.InsertChild(c, Matrix4d.RotateY(1.2) * Matrix4d.CreateTranslation(-1.5, -0.8, 0.0));
c.SetAttribute(PropertyName.MATERIAL, pm);
c = new Cube();
root.InsertChild(c, Matrix4d.RotateY(1.8) * Matrix4d.CreateTranslation(0.5, -0.8, 0.0));
c.SetAttribute(PropertyName.MATERIAL, pm);
c = new Cube();
root.InsertChild(c, Matrix4d.RotateY(2.4) * Matrix4d.CreateTranslation(2.5, -0.8, 0.0));
c.SetAttribute(PropertyName.MATERIAL, pm);
c = new Cube();
root.InsertChild(c, Matrix4d.RotateY(3.0) * Matrix4d.CreateTranslation(4.5, -0.8, 0.0));
c.SetAttribute(PropertyName.MATERIAL, pm);

// The Back row.
c = new Cube();
root.InsertChild(c, Matrix4d.RotateX(3.5) * Matrix4d.CreateTranslation(-4.0, 1.0, 2.0));
c.SetAttribute(PropertyName.MATERIAL, pm);
c = new Cube();
root.InsertChild(c, Matrix4d.RotateX(3.0) * Matrix4d.CreateTranslation(-2.5, 1.0, 2.0));
c.SetAttribute(PropertyName.MATERIAL, pm);
c = new Cube();
root.InsertChild(c, Matrix4d.RotateX(2.5) * Matrix4d.CreateTranslation(-1.0, 1.0, 2.0));
c.SetAttribute(PropertyName.MATERIAL, pm);
c = new Cube();
root.InsertChild(c, Matrix4d.RotateX(2.0) * Matrix4d.CreateTranslation(0.5, 1.0, 2.0));
c.SetAttribute(PropertyName.MATERIAL, pm);
c = new Cube();
root.InsertChild(c, Matrix4d.RotateX(1.5) * Matrix4d.CreateTranslation(2.0, 1.0, 2.0));
c.SetAttribute(PropertyName.MATERIAL, pm);
c = new Cube();
root.InsertChild(c, Matrix4d.RotateX(1.0) * Matrix4d.CreateTranslation(3.5, 1.0, 2.0));
c.SetAttribute(PropertyName.MATERIAL, pm);
c = new Cube();
root.InsertChild(c, Matrix4d.RotateX(0.5) * Matrix4d.CreateTranslation(5.0, 1.0, 2.0));
c.SetAttribute(PropertyName.MATERIAL, pm);
