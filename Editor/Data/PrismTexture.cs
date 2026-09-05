using System;
using UnityEngine;

namespace Prism
{
    /// <summary>
    /// A texture stored inside a theme as PNG bytes, so a theme is a single portable file.
    /// </summary>
    [Serializable]
    internal class PrismTexture
    {
        [SerializeField]
        private string _id;
        [SerializeField]
        private int _width;
        [SerializeField]
        private int _height;
        [SerializeField]
        private byte[] _bytes;

        private Texture2D _decoded;

        public string Id => _id;
        public int ByteCount => _bytes?.Length ?? 0;

        public bool IsValid => !string.IsNullOrEmpty(_id) && ByteCount > 0;

        /// <summary>
        /// Decoded lazily, and again after every domain reload - the decoded texture is not
        /// serialized, only the bytes are.
        /// </summary>
        public Texture2D Texture
        {
            get
            {
                if (!IsValid) return null;

                //Unity null: destroyed between reloads.
                if (_decoded == null)
                {
                    _decoded = Decode();
                }

                return _decoded;
            }
        }

        public static PrismTexture From(Texture2D source)
        {
            if (source == null) return null;

            var bytes = Encode(source);
            if (bytes == null || bytes.Length == 0) return null;

            return new PrismTexture
            {
                _id = Guid.NewGuid().ToString("N"),
                _width = source.width,
                _height = source.height,
                _bytes = bytes,
                _decoded = null
            };
        }

        private Texture2D Decode()
        {
            var texture = new Texture2D(_width, _height) { hideFlags = HideFlags.HideAndDontSave };

            if (texture.LoadImage(_bytes)) return texture;

            UnityEngine.Object.DestroyImmediate(texture);

            return null;
        }

        private static byte[] Encode(Texture2D source)
        {
            // Project textures are usually compressed and not readable, so they have to go through
            // a render target first. ReadPixels reads the *active* target, which is the part that
            // is easy to get wrong: without setting it, the encoded bytes are whatever happened to
            // be bound at the time.
            var previousActive = RenderTexture.active;
            var renderTexture = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.sRGB);
            Texture2D readable = null;

            try
            {
                Graphics.Blit(source, renderTexture);
                RenderTexture.active = renderTexture;

                readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, mipChain: false)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };

                readable.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0);
                readable.Apply();

                return ImageConversion.EncodeToPNG(readable);
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);

                if (readable != null)
                {
                    UnityEngine.Object.DestroyImmediate(readable);
                }
            }
        }
    }
}
