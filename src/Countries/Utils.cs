using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using StbImageSharp;
using Countries.Templates;

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
}