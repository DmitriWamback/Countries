using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL;

using System.Text.Json;
using OpenTK.Mathematics;

namespace Countries.Templates {

    public class Polygon {

        public float[] Longitude, Latitude, CombinedLongLat;
        int vertexBufferObject, vertexArrayObject;
        public static float longitudeOffset = -90f;

        public static Polygon[] LoadCountry(string countryName) {

            
            JsonElement countryCoordinates = DevelopmentWindow.CountryGeoJson.Geometry[countryName].GetProperty("coordinates");        
            string polygonType = DevelopmentWindow.CountryGeoJson.PolygonType[countryName];

            float[] Longitude, Latitude;
            Longitude = new float[countryCoordinates[0].GetArrayLength()];
            Latitude = new float[countryCoordinates[0].GetArrayLength()];

            if (polygonType == "Polygon") {
                
                for (int i = 0; i < Longitude.Length; i++) {
                    Longitude[i] = float.Parse(countryCoordinates[0][i][0].ToString()) - longitudeOffset;
                    Latitude[i]  = float.Parse(countryCoordinates[0][i][1].ToString());
                }
                Polygon p = new Polygon(Longitude, Latitude);
                p.Initialize();
                return new Polygon[] { p };
            }
            List<Polygon> polygons = new List<Polygon>();

            for (int i = 0; i < countryCoordinates.GetArrayLength(); i++) { for (int j = 0; j < countryCoordinates[i].GetArrayLength(); j++) {

                List<float> MLongitude = new List<float>();
                List<float> MLatitude = new List<float>();

                for (int k = 0; k < countryCoordinates[i][j].GetArrayLength(); k++) {
                    
                    float latitude = float.Parse(countryCoordinates[i][j][k][1].ToString());
                    float longitude = float.Parse(countryCoordinates[i][j][k][0].ToString()) - longitudeOffset;

                    MLatitude.Add(latitude);
                    MLongitude.Add(longitude);
                }

                Polygon p = new Polygon(MLongitude.ToArray(), MLatitude.ToArray());
                p.Initialize();

                polygons.Add(p);
                }
            }
            return polygons.ToArray();
        }

        public static Polygon[] LoadCity(string cityName) {

            JsonElement cityCoordinates = DevelopmentWindow.CityGeoJson.Geometry[cityName].GetProperty("coordinates");
            List<Polygon> polygons = new List<Polygon>();

            float[] Longitude = new float[cityCoordinates[0].GetArrayLength()], 
                    Latitude  = new float[cityCoordinates[0].GetArrayLength()];

            for (int i = 0; i < cityCoordinates[0].GetArrayLength(); i++) {
                Longitude[i] = float.Parse(cityCoordinates[0][i][0].ToString()) - longitudeOffset;
                Latitude[i] = float.Parse(cityCoordinates[0][i][1].ToString());
            }
            Polygon p = new Polygon(Longitude, Latitude);
            p.Initialize();

            polygons.Add(p);

            return polygons.ToArray();
        }

        public Polygon(float[] longitude, float[] latitude, float height = 1) {
            Longitude = longitude;
            Latitude = latitude;
            CombinedLongLat = new float[Longitude.Length * 3];

            CombineCoordinates(height);
        }

        public Polygon() {}

        public static Polygon Empty() {
            return new Polygon();
        }

        private void CombineCoordinates(float height) {

            float maxLongitude = 0, maxLatitude = 0;
            for (int i = 0; i < Longitude.Length; i++) {
                maxLongitude += Longitude[i];
                maxLatitude += Latitude[i];
            }

            maxLatitude /= Longitude.Length;
            maxLongitude /= Longitude.Length;

            for (int i = 0; i < CombinedLongLat.Length/3; i++) {

                Vector3 pointOnSphere = PointToSphere(Latitude[i], Longitude[i]);

                CombinedLongLat[i * 3]     = (pointOnSphere.X) * height;
                CombinedLongLat[i * 3 + 1] = (pointOnSphere.Y) * height;
                CombinedLongLat[i * 3 + 2] = (pointOnSphere.Z) * height;
            }

            /* 
                Creating the vertex buffer
            */
        }

        bool isInitialized = false;

        public void Initialize() {

            if (isInitialized) return;

            vertexArrayObject = GL.GenVertexArray();
            vertexBufferObject = GL.GenBuffer();

            GL.BindVertexArray(vertexArrayObject);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, CombinedLongLat.Length * sizeof(float), CombinedLongLat, BufferUsageHint.StaticDraw);
            
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            isInitialized = true;
        }

        public static Vector3 PointToSphere(float latitude, float longitude) {

            float y =  (float)Math.Sin(latitude * 3.14159265358797f / 180f);
            float r =  (float)Math.Cos(latitude * 3.14159265358797f / 180f);
            float x =  (float)Math.Sin(longitude * 3.14159265358797f / 180f) * r;
            float z = -(float)Math.Cos(longitude * 3.14159265358797f / 180f) * r;

            return new Vector3(z, y, x);
        }

        public void Render(PrimitiveType type) {

            try {
                GL.BindVertexArray(vertexArrayObject);
                //GL.DrawArrays(PrimitiveType.Points, 0, CombinedLongLat.Length / 3);
                GL.DrawArrays(type, 0, CombinedLongLat.Length / 3);
            } catch(Exception e) {}
        }
    }

    /*

        OPENGL SHADER CLASS

    */

    public class Shader {

        public int program;
        public Shader(string shaderFolder) {

            string vertexShaderSource   = new StreamReader("src/shaders/"+shaderFolder+"/vMain.glsl").ReadToEnd();
            string fragmentShaderSource = new StreamReader("src/shaders/"+shaderFolder+"/fMain.glsl").ReadToEnd();

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

        public void SetFloat(string name, float a) {
            int location = GL.GetUniformLocation(program, name);
            GL.Uniform1(location, a);
        }
    }
} /* END COUNTRIES.TEMPLATES */