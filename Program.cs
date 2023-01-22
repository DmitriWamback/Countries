using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL;

using System.Text.Json;
using OpenTK.Mathematics;

class Polygon {


    public float[] Longitude, Latitude, CombinedLongLat;
    int vertexBufferObject, vertexArrayObject;

    public static Polygon[] LoadCountry(string countryName) {
        
        JsonElement countryCoordinates = DevelopmentWindow.CountryGeometry[countryName].GetProperty("coordinates");        
        string polygonType = DevelopmentWindow.CountryPolygonType[countryName];

        float[] Longitude, Latitude;
        Longitude = new float[countryCoordinates[0].GetArrayLength()];
        Latitude = new float[countryCoordinates[0].GetArrayLength()];

        if (polygonType == "Polygon") {
            
            for (int i = 0; i < Longitude.Length; i++) {
                Longitude[i] = float.Parse(countryCoordinates[0][i][0].ToString());
                Latitude[i]  = float.Parse(countryCoordinates[0][i][1].ToString());
            }
            return new Polygon[] { new Polygon(Longitude, Latitude) };
        }
        List<Polygon> polygons = new List<Polygon>();

        for (int i = 0; i < countryCoordinates.GetArrayLength(); i++) { for (int j = 0; j < countryCoordinates[i].GetArrayLength(); j++) {

            List<float> MLongitude = new List<float>();
            List<float> MLatitude = new List<float>();

            for (int k = 0; k < countryCoordinates[i][j].GetArrayLength(); k++) {
                
                float latitude = float.Parse(countryCoordinates[i][j][k][1].ToString());
                float longitude = float.Parse(countryCoordinates[i][j][k][0].ToString());

                MLatitude.Add(latitude);
                MLongitude.Add(longitude);
            }
            polygons.Add(new Polygon(MLongitude.ToArray(), MLatitude.ToArray()));
            }
        }
        return polygons.ToArray();
    }

    public Polygon(float[] longitude, float[] latitude) {
        Longitude = longitude;
        Latitude = latitude;
        CombinedLongLat = new float[Longitude.Length * 3];

        CombineCoordinates();
    }

    private void CombineCoordinates() {

        float maxLongitude = 0, maxLatitude = 0;
        for (int i = 0; i < Longitude.Length; i++) {
            maxLongitude += Longitude[i];
            maxLatitude += Latitude[i];
        }

        maxLatitude /= Longitude.Length;
        maxLongitude /= Longitude.Length;

        for (int i = 0; i < CombinedLongLat.Length/3; i++) {

            Vector3 pointOnSphere = PointToSphere(Latitude[i], Longitude[i]);

            CombinedLongLat[i * 3]     = (pointOnSphere.X);
            CombinedLongLat[i * 3 + 1] = (pointOnSphere.Y);
            CombinedLongLat[i * 3 + 2] = (pointOnSphere.Z);
        }

        /* 
            Creating the vertex buffer
        */

        vertexArrayObject = GL.GenVertexArray();
        vertexBufferObject = GL.GenBuffer();

        GL.BindVertexArray(vertexArrayObject);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, CombinedLongLat.Length * sizeof(float), CombinedLongLat, BufferUsageHint.StaticDraw);
        
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
    }
    public static Vector3 PointToSphere(float latitude, float longitude) {

        float y =  (float)Math.Sin(latitude * 3.14159265358797f / 180f);
        float r =  (float)Math.Cos(latitude * 3.14159265358797f / 180f);
        float x =  (float)Math.Sin(longitude * 3.14159265358797f / 180f) * r;
        float z = -(float)Math.Cos(longitude * 3.14159265358797f / 180f) * r;

        return new Vector3(z, y, x);
    }

    public void Render() {

        GL.BindVertexArray(vertexArrayObject);
        GL.DrawArrays(PrimitiveType.LineLoop, 0, CombinedLongLat.Length / 3);
    }
}

/*

    OPENGL SHADER CLASS

*/

class Shader {

    public int program;
    public Shader() {

        string vertexShaderSource   = new StreamReader("shaders/vMain.glsl").ReadToEnd();
        string fragmentShaderSource = new StreamReader("shaders/fMain.glsl").ReadToEnd();

        int vertex = GL.CreateShader(ShaderType.VertexShader);
        int fragment = GL.CreateShader(ShaderType.FragmentShader);

        GL.ShaderSource(vertex, vertexShaderSource);
        GL.ShaderSource(fragment, fragmentShaderSource);
        GL.CompileShader(vertex);
        GL.CompileShader(fragment);

        Console.WriteLine(GL.GetShaderInfoLog(vertex));
        Console.WriteLine(GL.GetShaderInfoLog(fragment));

        program = GL.CreateProgram();
        GL.AttachShader(program, vertex);
        GL.AttachShader(program, fragment);
        GL.LinkProgram(program);
    }

    public void Use() {
        GL.UseProgram(program);
    }

    public void SetMatrix4(string name, Matrix4 matr) {
        int location = GL.GetUniformLocation(program, name);
        GL.UniformMatrix4(location, false, ref matr);
    }

    public void SetVector3(string name, Vector3 a) {
        int location = GL.GetUniformLocation(program, name);
        GL.Uniform3(location, a.X, a.Y, a.Z);
    }
}

/*

    MAIN WINDOW TO DISPLAY THE GLOBE AND COUNTRIES

*/

class DevelopmentWindow: GameWindow {

    List<Polygon> _countryPolygons = new List<Polygon>();
    Vector3 currentCountryPosition = new Vector3(0, 0, 2),
            rotation = new Vector3(0);

    public static Dictionary<string, JsonElement> CountryGeometry = new Dictionary<string, JsonElement>();
    public static Dictionary<string, string> CountryPolygonType = new Dictionary<string, string>();
    public List<string> debugCountryNames = new List<string>();
    float globeZoom = 2.24f;

    Polygon[] countryPolygons = new Polygon[1];

    JsonElement GeoDocument;
    Shader? countryShader = null;

    Matrix4 projection, lookAt, rotationMatrix;

    public DevelopmentWindow(GameWindowSettings GWS, NativeWindowSettings NWS) : base(GWS, NWS) { Run(); }

    
    protected override void OnLoad() {
        base.OnLoad();

        GL.Enable(EnableCap.ProgramPointSize);

        var jsonFile = new StreamReader("countries.geojson").ReadToEnd();
        GeoDocument = JsonDocument.Parse(jsonFile).RootElement.GetProperty("features");

        int length = GeoDocument.GetArrayLength();

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

        projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(110), 1, 0.01f, 1000f);
        lookAt = Matrix4.LookAt(currentCountryPosition, new Vector3(0), Vector3.UnitY);

        string[] countriesToLoad = new string[] {
            "India"
        };

        foreach (string str in countriesToLoad) {
            countryPolygons = Polygon.LoadCountry(str);
            foreach(Polygon p in countryPolygons) {
                _countryPolygons.Add(p);
            }
        }
        countryShader = new Shader();
    }

    /*
        KEYBOARD INPUT
    */

    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);
        string[] countryNames = CountryPolygonType.Keys.ToArray();
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
            rotation.X -= this.MouseState.Delta.X * 0.001f * globeZoom;
            rotation.Y += this.MouseState.Delta.Y * 0.001f * globeZoom;

            if (rotation.Y > 1.54362f) rotation.Y = 1.54362f;
            if (rotation.Y < -1.54362f) rotation.Y = -1.54362f;

            currentCountryPosition = new Vector3(MathF.Sin(rotation.X) * MathF.Cos(rotation.Y) * globeZoom, MathF.Sin(rotation.Y) * globeZoom, MathF.Cos(rotation.X) * MathF.Cos(rotation.Y) * globeZoom);
        }
    }
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        globeZoom += e.OffsetY * 0.01f;
        if (globeZoom < 1.05f) globeZoom = 1.05f;
        currentCountryPosition = new Vector3(MathF.Sin(rotation.X) * MathF.Cos(rotation.Y) * globeZoom, MathF.Sin(rotation.Y) * globeZoom, MathF.Cos(rotation.X) * MathF.Cos(rotation.Y) * globeZoom);
    }

    /*
        MAIN LOOP
    */

    protected override void OnUpdateFrame(FrameEventArgs args) {
        base.OnUpdateFrame(args);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.Viewport(0, -400, 2400, 2400);
        GL.ClearColor(0, 0, 0, 1);

        lookAt = Matrix4.LookAt(currentCountryPosition, new Vector3(0), Vector3.UnitY);
        countryShader.Use();
        countryShader.SetMatrix4("projection", projection);
        countryShader.SetMatrix4("lookAt", lookAt);
        countryShader.SetMatrix4("rotation", rotationMatrix);
        countryShader.SetVector3("cameraPosition", currentCountryPosition);

        foreach (Polygon polygon in _countryPolygons) {
            polygon.Render();
        }

        this.SwapBuffers();
        GLFW.PollEvents();
    }
}


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
