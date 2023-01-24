using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

using System.Text.Json;
using System.Runtime.InteropServices;

using Countries;
using Countries.Templates;
using Countries.Util;

namespace Countries {

    class DevelopmentWindow: GameWindow {

        List<Polygon> _countryPolygons = new List<Polygon>();
        Vector3 currentCountryPosition = new Vector3(0, 0, 2),
                rotation = new Vector3(0);

        public static Dictionary<string, JsonElement> CountryGeometry = new Dictionary<string, JsonElement>();
        public static Dictionary<string, string> CountryPolygonType = new Dictionary<string, string>();
        public List<string> debugCountryNames = new List<string>();
        float globeZoom = 2.24f, t = 0f;

        Polygon[] countryPolygons = new Polygon[1];

        JsonElement GeoDocument;
        Shader sphereShader, borderShader;

        Matrix4 projection, lookAt, rotationMatrix;
        Sphere debugSphere;
        Texture surfaceTexture;

        public DevelopmentWindow(GameWindowSettings GWS, NativeWindowSettings NWS) : base(GWS, NWS) { Run(); }
        
        protected override void OnLoad() {
            base.OnLoad();

            GL.Enable(EnableCap.ProgramPointSize);

            var jsonFile = new StreamReader("src/resources/countries.geojson").ReadToEnd();
            GeoDocument = JsonDocument.Parse(jsonFile).RootElement.GetProperty("features");

            int length = GeoDocument.GetArrayLength();
            debugSphere = Utils.createSphere();
            surfaceTexture = new Texture("src/resources/earthsurface.jpeg");

            /*
                Loading the geojson
            */
            
            for (int i = 0; i < length; i++) {
                JsonElement elem = GeoDocument[i];
                JsonElement countryName = elem.GetProperty("properties").GetProperty("ADMIN"),
                            countryGeometry = elem.GetProperty("geometry"),
                            geometryType    = elem.GetProperty("geometry").GetProperty("type");

                            debugCountryNames.Add(countryName.ToString());
                
                CountryGeometry[countryName.ToString()] = countryGeometry;
                CountryPolygonType[countryName.ToString()] = geometryType.ToString();
            }

            projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(110), 1, 0.001f, 1000f);
            lookAt = Matrix4.LookAt(currentCountryPosition, new Vector3(0), Vector3.UnitY);


            // Loading country borders
            foreach (string str in CountryGeometry.Keys) {
                countryPolygons = Polygon.LoadCountry(str);
                foreach(Polygon p in countryPolygons) {
                    _countryPolygons.Add(p);
                }
            }
            sphereShader = new Shader("sphere");
            borderShader = new Shader("borders");

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.LineSmooth);
        }

        /*
            KEYBOARD INPUT
        */

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);
            string[] countryNames = CountryPolygonType.Keys.ToArray();

            if (e.Key == Keys.D) {t += 0.01f; Console.WriteLine(t);}
            if (e.Key != Keys.E) return;

            try {
                int random = new Random().Next(0, debugCountryNames.Count);

                string countryName = debugCountryNames[random];
                debugCountryNames.Remove(debugCountryNames[random]);

                Polygon[] polygons = Polygon.LoadCountry(countryName);
                foreach(Polygon polygon in polygons) {
                    _countryPolygons.Add(polygon);
                }
                Console.WriteLine(countryName);
            } catch(Exception exce) {}
        }
        protected override void OnMouseMove(MouseMoveEventArgs e) {
            base.OnMouseMove(e);

            if (this.IsMouseButtonDown(MouseButton.Right)) {
                rotation.X -= this.MouseState.Delta.X * 0.0001f * MathF.Pow(globeZoom, 6);
                rotation.Y += this.MouseState.Delta.Y * 0.0001f * MathF.Pow(globeZoom, 6);

                if (rotation.Y > 1.54362f) rotation.Y = 1.54362f;
                if (rotation.Y < -1.54362f) rotation.Y = -1.54362f;

                currentCountryPosition = new Vector3(MathF.Sin(rotation.X) * MathF.Cos(rotation.Y) * globeZoom, 
                                                     MathF.Sin(rotation.Y) * globeZoom, 
                                                     MathF.Cos(rotation.X) * MathF.Cos(rotation.Y) * globeZoom);
            }
        }
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            globeZoom += e.OffsetY * 0.01f;

            if (globeZoom < 1.061f) globeZoom = 1.061f;
            if (globeZoom > 1.5030011f) globeZoom = 1.5030011f;

            currentCountryPosition = new Vector3(MathF.Sin(rotation.X) * MathF.Cos(rotation.Y) * globeZoom, 
                                                 MathF.Sin(rotation.Y) * globeZoom, 
                                                 MathF.Cos(rotation.X) * MathF.Cos(rotation.Y) * globeZoom);
        }

        protected override void OnUpdateFrame(FrameEventArgs args) {
            base.OnUpdateFrame(args);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            int width = this.ClientSize.X, 
                height = this.ClientSize.Y;
            
            GL.Viewport(0, -Math.Abs(width - height) / 2, width, width);

            GL.ClearColor(0, 0, 0, 1);

            lookAt = Matrix4.LookAt(currentCountryPosition, new Vector3(0), Vector3.UnitY);
            borderShader.Use();
            borderShader.SetMatrix4("projection", projection);
            borderShader.SetMatrix4("lookAt", lookAt);
            borderShader.SetMatrix4("rotation", rotationMatrix);
            borderShader.SetVector3("cameraPosition", currentCountryPosition);

            foreach (Polygon polygon in _countryPolygons) {
                polygon.Render();
            }

            surfaceTexture.Bind();
            sphereShader.Use();
            sphereShader.SetFloat("time", t);
            sphereShader.SetMatrix4("projection", projection);
            sphereShader.SetMatrix4("lookAt", lookAt);
            sphereShader.SetMatrix4("rotation", rotationMatrix);
            sphereShader.SetVector3("cameraPosition", currentCountryPosition);
            debugSphere.Render();

            this.SwapBuffers();
            GLFW.PollEvents();
        }
    }
} /* END COUNTRIES */


class Program {

    public static void Main() {
        GameWindowSettings gws = new GameWindowSettings();
        NativeWindowSettings nws = new NativeWindowSettings() {
            APIVersion  = new Version(4, 1),
            Profile     = ContextProfile.Core,
            Flags       = ContextFlags.ForwardCompatible,
            Title       = "Globe",
            Size        = new Vector2i(1200, 800),
            NumberOfSamples = 14
        };
        new DevelopmentWindow(gws, nws);
    }
}