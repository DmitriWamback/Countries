using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

using System.Text.Json;

using Countries;
using Countries.Templates;
using Countries.Util;
using System.Net.Http;

namespace Countries {

    class DevelopmentWindow: GameWindow {

        Vector3 currentCountryPosition = new Vector3(0, 0, 1.5030011f),
                rotation = new Vector3(0);

        public struct GeoJson {
            public Dictionary<string, JsonElement> Geometry = new Dictionary<string, JsonElement>();
            public Dictionary<string, string> PolygonType   = new Dictionary<string, string>();
            public JsonElement GeoDocument;
            public List<Polygon> Polygons = new List<Polygon>();

            public GeoJson() {}
        }
        public struct Points {
            public Vector3 Color;
            public Polygon Coordinates;
        }

        public static GeoJson CountryGeoJson, CityGeoJson;
        public static Points Planes, Ships;

        public static float globeZoom = 1.5030011f, t = 0f;

        Shader sphereShader, borderShader;
        Matrix4 projection, lookAt, rotationMatrix;
        Sphere debugSphere;
        Texture surfaceTexture;

        public DevelopmentWindow(GameWindowSettings GWS, NativeWindowSettings NWS) : base(GWS, NWS) { Run(); }
        
        protected override void OnLoad() {
            base.OnLoad();

            GL.Enable(EnableCap.ProgramPointSize);

            CountryGeoJson = new GeoJson();
            CityGeoJson    = new GeoJson();

            Planes = new Points();
            Ships  = new Points();
            
            Planes.Color = new Vector3(1, 0, 0);
            Ships.Color  = new Vector3(0, 1, 0);

            CountryGeoJson.GeoDocument = JsonDocument.Parse(new StreamReader("src/resources/countries.geojson").ReadToEnd()).RootElement.GetProperty("features");
            CityGeoJson.GeoDocument    = JsonDocument.Parse(new StreamReader("src/resources/cities.geojson").ReadToEnd()).RootElement.GetProperty("features");

            debugSphere = Utils.createSphere();
            surfaceTexture = new Texture("src/resources/earthsurface.jpeg");
            Task.Run(() => UpdatePlanes());

            /*
                Loading the country geojson
            */
            for (int i = 0; i < CountryGeoJson.GeoDocument.GetArrayLength(); i++) {
                JsonElement elem = CountryGeoJson.GeoDocument[i];
                JsonElement countryName = elem.GetProperty("properties").GetProperty("ADMIN"),
                            countryGeometry = elem.GetProperty("geometry"),
                            geometryType    = elem.GetProperty("geometry").GetProperty("type");
                
                CountryGeoJson.Geometry[countryName.ToString()] = countryGeometry;
                CountryGeoJson.PolygonType[countryName.ToString()] = geometryType.ToString();
            }

            /*
                Loading the city geojson
            */
            for (int i = 0; i < CityGeoJson.GeoDocument.GetArrayLength(); i++) {
                JsonElement main = CityGeoJson.GeoDocument[i];
                JsonElement cityName = main.GetProperty("properties").GetProperty("NAME"),
                            cityGeometry = main.GetProperty("geometry");

                CityGeoJson.Geometry[cityName.ToString()] = cityGeometry;
            }


            projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(110f), 1, 0.001f, 1000f);
            lookAt = Matrix4.LookAt(currentCountryPosition, new Vector3(0f), Vector3.UnitY);

            // Loading country borders
            foreach (string str in CountryGeoJson.Geometry.Keys) {
                Polygon[] loadedPolygons = Polygon.LoadCountry(str);
                foreach(Polygon p in loadedPolygons) {
                    CountryGeoJson.Polygons.Add(p);
                }
            }
            Console.WriteLine("Loaded Countries");
            foreach (string name in CityGeoJson.Geometry.Keys) {
                Polygon[] loadedCities = Polygon.LoadCity(name);
                foreach(Polygon city in loadedCities) {
                    CityGeoJson.Polygons.Add(city);
                }
            }

            if(Planes.Coordinates != null) Planes.Coordinates.Initialize();

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
            if (e.Key == Keys.D) {t -= 0.001f; Console.WriteLine(t);}
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

            if (globeZoom < 1.061f)     globeZoom = 1.061f;
            if (globeZoom > 1.5030011f) globeZoom = 1.5030011f;

            currentCountryPosition = new Vector3(MathF.Sin(rotation.X) * MathF.Cos(rotation.Y) * globeZoom, 
                                                 MathF.Sin(rotation.Y) * globeZoom, 
                                                 MathF.Cos(rotation.X) * MathF.Cos(rotation.Y) * globeZoom);
        }

        bool UpdatePlanesFlag = false, UpdateISSFlag = false;
        public static Polygon ISS;

        public async void UpdatePlanes() {
            
            try {
                HttpClient client = new HttpClient();
                string response = await client.GetStringAsync("https://opensky-network.org/api/states/all");
                Planes.Coordinates = Http.GetCoordinatesFromJson(response, Http.TrackType.OpenskyAPI);
            } catch(Exception e) { Console.WriteLine("OpenSky too many requests"); }
        }

        protected override void OnUpdateFrame(FrameEventArgs args) {
            
            string estTimeZone = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now).ToString("hh:mm:ss:tt");
            string[] times = estTimeZone.Split(":");
            float hour = float.Parse(times[0]) * 60f;
            float minute = float.Parse(times[1]);
            float seconds = float.Parse(times[2]) / 60f;
            string AMPM = times[3];

            if (seconds * 60 % 2 == 1 && !UpdateISSFlag) {
                Task.Run(() => Http.GetISS());
                UpdateISSFlag = true;
            }

            if (seconds * 60 % 30 == 1 && !UpdatePlanesFlag) {
                Task.Run(() => UpdatePlanes());
                UpdatePlanesFlag = true;
            }

            if ((int)(seconds * 60) % 30 == 0) UpdatePlanesFlag = false;
            if (seconds * 60 % 2 == 0) UpdateISSFlag = false;

            if (Planes.Coordinates != null) Planes.Coordinates.Initialize();
            if (ISS != null) ISS.Initialize();

            float time = AMPM == "AM" ? (hour + minute + seconds + 12f * 60f) / 60f / 24f : (hour + minute + seconds) / 60f / 24f;
            float rotation = time * 360f - 45f;

            base.OnUpdateFrame(args);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            int width  = this.ClientSize.X, 
                height = this.ClientSize.Y;
            
            GL.Viewport(0, -Math.Abs(width - height) / 2, width, width);

            GL.ClearColor(0, 0, 0, 1);

            t -= 0.0001f;
            rotationMatrix = GeoMath.EulerRotation(new Vector3(0, -rotation * 3.14159265358797f / 180f, 0));

            lookAt = Matrix4.LookAt(currentCountryPosition, new Vector3(0), Vector3.UnitY);
            borderShader.Use();
            borderShader.SetMatrix4("projection", projection);
            borderShader.SetMatrix4("lookAt", lookAt);
            borderShader.SetMatrix4("rotation", rotationMatrix);
            borderShader.SetVector3("cameraPosition", currentCountryPosition);

            borderShader.SetVector3("color", new Vector3(1));
            foreach (Polygon polygon in CountryGeoJson.Polygons) {
                polygon.Render(PrimitiveType.LineLoop);
            }

            borderShader.SetVector3("color", new Vector3(1.0f, 1.0f, 0f));
            foreach(Polygon city in CityGeoJson.Polygons) {
                city.Render(PrimitiveType.TriangleFan);
            }
            borderShader.SetFloat("pointSize", 4);
            borderShader.SetVector3("color", Planes.Color);
            try {
                Planes.Coordinates.Render(PrimitiveType.Points);
            } catch(Exception e) {}

            borderShader.SetFloat("pointSize", 10);
            borderShader.SetVector3("color", Ships.Color);
            try {
                ISS.Render(PrimitiveType.Points);
            } catch(Exception e) {}

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