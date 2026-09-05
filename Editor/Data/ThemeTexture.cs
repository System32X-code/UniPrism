using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UniPrism
{
    /// <summary>
    /// An image stored inside a theme as encoded bytes, so a theme is a single portable file.
    /// </summary>
    [Serializable]
    internal class ThemeTexture
    {
        [SerializeField]
        private string _id;
        [SerializeField]
        private int _width;
        [SerializeField]
        private int _height;
        [SerializeField]
        private byte[] _bytes;
        [SerializeField]
        private bool _fromSourceFile;

        private Texture2D _decoded;

        public string Id => _id;
        public int ByteCount => _bytes?.Length ?? 0;

        /// <summary>Whether the bytes are the original file rather than a re-encode.</summary>
        public bool FromSourceFile => _fromSourceFile;

        public bool IsValid => !string.IsNullOrEmpty(_id) && ByteCount > 0;

        /// <summary>
        /// Decoded lazily, and again after every domain reload: only the bytes are serialized.
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

        /// <summary>
        /// Builds from a file on disk, with no import into the project.
        /// </summary>
        /// <remarks>
        /// A theme stores image bytes, not an asset reference, so an image never needed to be a
        /// project asset in the first place. Going straight to the file also avoids the importer
        /// entirely - no compression, no maxTextureSize cap - and keeps a wallpaper out of a
        /// project it has nothing to do with.
        /// </remarks>
        public static ThemeTexture FromFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(path);
            }
            catch (Exception)
            {
                return null;
            }

            if (bytes.Length == 0) return null;

            //Decoded once up front: it both validates the file and recovers its real dimensions.
            var probe = new Texture2D(2, 2) { hideFlags = HideFlags.HideAndDontSave };
            int width, height;
            try
            {
                if (!probe.LoadImage(bytes)) return null;

                width = probe.width;
                height = probe.height;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(probe);
            }

            return new ThemeTexture
            {
                _id = Guid.NewGuid().ToString("N"),
                _width = width,
                _height = height,
                _bytes = bytes,
                _fromSourceFile = true
            };
        }

        public static ThemeTexture From(Texture2D source)
        {
            if (source == null) return null;

            // Preferred by a wide margin: the file on disk is the picture the user chose. What
            // Unity hands back as a Texture2D has already been through the importer, which
            // compresses it and caps it at maxTextureSize - so re-encoding that is a copy of a
            // copy, and it shows.
            var bytes = ReadSourceFile(source, out var width, out var height);
            var fromSourceFile = bytes != null;

            if (!fromSourceFile)
            {
                bytes = ReEncode(source);
                width = source.width;
                height = source.height;
            }

            if (bytes == null || bytes.Length == 0) return null;

            return new ThemeTexture
            {
                _id = Guid.NewGuid().ToString("N"),
                _width = width,
                _height = height,
                _bytes = bytes,
                _fromSourceFile = fromSourceFile
            };
        }

        /// <summary>
        /// The asset's own bytes, when it is an image file this can decode later.
        /// </summary>
        private static byte[] ReadSourceFile(Texture2D source, out int width, out int height)
        {
            width = source.width;
            height = source.height;

            try
            {
                var path = AssetDatabase.GetAssetPath(source);
                if (string.IsNullOrEmpty(path)) return null;

                var extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension != ".png" && extension != ".jpg" && extension != ".jpeg") return null;

                //Package and built-in assets are not necessarily files on disk.
                if (!File.Exists(path)) return null;

                var bytes = File.ReadAllBytes(path);
                if (bytes.Length == 0) return null;

                //The importer may have capped the size, so the file's own dimensions are recovered
                //by decoding it once rather than trusted from the imported texture.
                var probe = new Texture2D(2, 2) { hideFlags = HideFlags.HideAndDontSave };
                try
                {
                    if (probe.LoadImage(bytes))
                    {
                        width = probe.width;
                        height = probe.height;
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(probe);
                }

                return bytes;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Fallback for a texture with no readable file behind it.
        /// </summary>
        private static byte[] ReEncode(Texture2D source)
        {
            // Project textures are usually compressed and not readable, so they have to go through
            // a render target. ReadPixels reads the *active* target, which is easy to get wrong:
            // without setting it the encoded bytes are whatever happened to be bound at the time.
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

                if (readable != null) UnityEngine.Object.DestroyImmediate(readable);
            }
        }

        private Texture2D Decode()
        {
            // No mip chain: a background is drawn at or near full size, and mipmaps only give the
            // sampler a smaller image to blur towards.
            var texture = new Texture2D(_width, _height, TextureFormat.RGBA32, mipChain: false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0
            };

            if (texture.LoadImage(_bytes, markNonReadable: false)) return texture;

            UnityEngine.Object.DestroyImmediate(texture);

            return null;
        }
    }
}
