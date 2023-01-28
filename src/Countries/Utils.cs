using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;

using StbImageSharp;

using Countries.Templates;
using RestSharp;
using System.Text.Json;

namespace Countries.Util {
    
    public class Utils {

        public static Sphere createSphere() {

            List<float> points = new List<float>();

            int[] offsets = {
                0, 0,
                0, 1,
                1, 1,

                0, 0,
                1, 0,
                1, 1,
            };

            for (int i = -180; i <= 180; i++) { for (int j = -90; j <= 90; j++) {
                    
                float longitude = (float)i;
                float latitude  = (float)j;

                for (int o = 0; o < offsets.Length/2; o++) {
                    Vector3 pointOnSphere1 = Polygon.PointToSphere(latitude + offsets[o * 2 + 1], longitude + offsets[o * 2]);
                    points.Add(pointOnSphere1.X);
                    points.Add(pointOnSphere1.Y);
                    points.Add(pointOnSphere1.Z);
                }
            }
            }

            return new Sphere(points.ToArray());
        }
    }

    public class Sphere {

        float[] coordinates;
        int vertexBufferObject, vertexArrayObject;

        public Sphere(float[] coords) {
            coordinates = coords;

            vertexArrayObject = GL.GenVertexArray();
            vertexBufferObject = GL.GenBuffer();

            GL.BindVertexArray(vertexArrayObject);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, coords.Length * sizeof(float), coords, BufferUsageHint.StaticDraw);
            
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
        }

        public void Render() {

            GL.BindVertexArray(vertexArrayObject);
            GL.DrawArrays(PrimitiveType.Triangles, 0, coordinates.Length / 3);
        }
    }

    public class GeoMath {

        public static Matrix4 EulerRotation(Vector3 rotation) {

            float x = rotation.X;
            float y = rotation.Y;
            float z = rotation.Z;

            Matrix4 xRotation = new Matrix4(
                new Vector4(MathF.Cos(x), -MathF.Sin(x), 0, 0),
                new Vector4(MathF.Sin(x),  MathF.Cos(x), 0, 0),
                new Vector4(0,             0,            1, 0),
                new Vector4(0,             0,            0, 1)
            );
            Matrix4 yRotation = new Matrix4(
                new Vector4( MathF.Cos(y), 0, MathF.Sin(y), 0),
                new Vector4( 0,            1, 0,            0),
                new Vector4(-MathF.Sin(y), 0, MathF.Cos(y), 0),
                new Vector4( 0,            0, 0,            1)
            );
            Matrix4 zRotation = new Matrix4(
                new Vector4(1, 0,             0,            0),
                new Vector4(0, MathF.Cos(z), -MathF.Sin(z), 0),
                new Vector4(0, MathF.Sin(z),  MathF.Cos(z), 0),
                new Vector4(0, 0,             0,            1)
            );

            return xRotation * yRotation * zRotation;
        }
    }

    public class Texture {

        int texture;

        public Texture(string textureFile) {

            using(var stream = File.OpenRead(textureFile)) {
                ImageResult img = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

                texture = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, texture);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureParameterName.ClampToBorder);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureParameterName.ClampToBorder);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapR, (int)TextureParameterName.ClampToBorder);

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, img.Width, img.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, img.Data);
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }
        }

        public void Bind() {
            GL.BindTexture(TextureTarget.Texture2D, texture);
        }
    }

    public class Http {

        public enum TrackType {
            OpenskyAPI
        }

        public static Polygon GetCoordinates(string httpUrl, TrackType type) {
            
            RestClient client = new RestClient();
            RestRequest request = new RestRequest(httpUrl);
            RestResponse response = client.Get(request);
            
            return GetCoordinatesFromJson(response.Content!, TrackType.OpenskyAPI);
        }

        public static Polygon GetCoordinatesFromJson(string json, TrackType type) {

            JsonElement main = JsonDocument.Parse(json).RootElement;
        
            Polygon coordinates = Polygon.Empty();
            List<float> latitude = new List<float>(), longitude = new List<float>();

            if (type == TrackType.OpenskyAPI) {
                
                float height = 1.0001f;
                int length = main.GetProperty("states").GetArrayLength();

                for (int i = 0; i < length; i++) {
                    
                    try {
                        latitude.Add(float.Parse(main.GetProperty("states")[i][5].ToString()));
                        longitude.Add(float.Parse(main.GetProperty("states")[i][6].ToString()));
                    } catch(Exception e){}
                }
                coordinates = new Polygon(latitude.ToArray(), longitude.ToArray(), height);
            }

            return coordinates;
        }
    }
}