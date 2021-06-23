using engenious;
using engenious.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace OctoAwesome.UI.Components
{
    public sealed class AssetComponent : DrawableGameComponent
    {
        private readonly ISettings settings;

        public const string INFOFILENAME = "packinfo.xml";

        public const string SETTINGSKEY = "ActiveResourcePacks";

        public const string RESOURCEPATH = "Resources";
        readonly Dictionary<string, Texture2D> textures;
        readonly Dictionary<string, Bitmap> bitmaps;
        readonly string[] textureTypes = new string[] { "png", "jpg", "jpeg", "bmp" };
        readonly List<ResourcePack> loadedPacks = new();
        readonly List<ResourcePack> activePacks = new();

        /// <summary>
        /// Gibt an, ob der Asset Manager bereit zum Laden von Resourcen ist.
        /// </summary>
        public bool Ready { get; private set; }

        /// <summary>
        /// Gibt die Anzahl geladener Texturen zurück.
        /// </summary>
        public int LoadedTextures => textures.Count + bitmaps.Count;

        /// <summary>
        /// Auflistung aller bekannten Resource Packs.
        /// </summary>
        public IEnumerable<ResourcePack> LoadedResourcePacks => loadedPacks.AsEnumerable();

        /// <summary>
        /// Auflistung aller aktuell aktiven Resource Packs.
        /// </summary>
        public IEnumerable<ResourcePack> ActiveResourcePacks => activePacks.AsEnumerable();

        public AssetComponent(IGame game, ISettings settings) : base(game)
        {
            this.settings = settings;

            Ready = false;
            textures = new Dictionary<string, Texture2D>();
            bitmaps = new Dictionary<string, Bitmap>();
            ScanForResourcePacks();

            // Load list of active Resource Packs
            List<ResourcePack> toLoad = new List<ResourcePack>();
            if (settings.KeyExists(SETTINGSKEY))
            {
                string activePackPathes = settings.Get<string>(SETTINGSKEY);
                if (!string.IsNullOrEmpty(activePackPathes))
                {
                    string[] packPathes = activePackPathes.Split(';');
                    foreach (var packPath in packPathes)
                    {
                        ResourcePack resourcePack = loadedPacks.FirstOrDefault(p => p.Path.Equals(packPath));
                        if (resourcePack != null) toLoad.Add(resourcePack);
                    }
                }
            }

            ApplyResourcePacks(toLoad);
        }

        public void ScanForResourcePacks()
        {
            loadedPacks.Clear();
            if (Directory.Exists(RESOURCEPATH))
            {
                foreach (var directory in Directory.GetDirectories(RESOURCEPATH))
                {
                    DirectoryInfo info = new DirectoryInfo(directory);
                    if (File.Exists(Path.Combine(directory, INFOFILENAME)))
                    {
                        // Scan info File
                        XmlSerializer serializer = new XmlSerializer(typeof(ResourcePack));
                        using Stream stream = File.OpenRead(Path.Combine(directory, INFOFILENAME));
                        ResourcePack pack = (ResourcePack)serializer.Deserialize(stream);
                        pack!.Path = info.FullName;
                        loadedPacks.Add(pack);
                    }
                    else
                    {
                        ResourcePack pack = new ResourcePack()
                        {
                            Path = info.FullName,
                            Name = info.Name
                        };

                        loadedPacks.Add(pack);
                    }
                }
            }
        }

        public void ApplyResourcePacks(IEnumerable<ResourcePack> packs)
        {
            Ready = false;

            // Reset vorhandener Assets
            foreach (var component in Game.Components.OfType<IAssetRelatedComponent>())
                component.UnloadAssets();

            // Dispose Bitmaps
            lock (bitmaps)
            {
                foreach (var value in bitmaps.Values)
                    value.Dispose();
                bitmaps.Clear();
            }

            // Dispose textures
            lock (textures)
            {
                foreach (var value in textures.Values)
                    value.Dispose();
                textures.Clear();
            }

            // Set new Active Resource Packs
            activePacks.Clear();
            foreach (var pack in packs)
                if (loadedPacks.Contains(pack)) // Warum eigentlich keine eigenen Packs?
                    activePacks.Add(pack);

            // Signal zum Reload senden
            foreach (var component in Game.Components.OfType<IAssetRelatedComponent>())
                component.ReloadAssets();

            // Speichern der Settings
            settings.Set(SETTINGSKEY, string.Join(";", activePacks.Select(p => p.Path)));

            Ready = true;
        }
        public Texture2D LoadTexture(string key)
        {
            lock (textures)
            {
                return Load(typeof(AssetComponent), key, textureTypes, textures, (stream) => Texture2D.FromStream(GraphicsDevice, stream));
            }
        }

        public Texture2D LoadTexture(Type baseType, string key)
        {
            lock (textures)
            {
                return Load(baseType, key, textureTypes, textures, (stream) => Texture2D.FromStream(GraphicsDevice, stream));
            }
        }

        public Bitmap LoadBitmap(Type baseType, string key)
        {
            lock (bitmaps)
            {
                return Load(baseType, key, textureTypes, bitmaps, (stream) => (Bitmap)Image.FromStream(stream));
            }
        }

        public Stream LoadStream(Type baseType, string key, params string[] fileTypes)
        {
            return Load(baseType, key, fileTypes, null, (stream) =>
            {
                MemoryStream result = new MemoryStream();
                byte[] buffer = new byte[1024];
                int count = 0;
                do
                {
                    count = stream.Read(buffer, 0, buffer.Length);
                    result.Write(buffer, 0, count);
                } while (count != 0);
                result.Seek(0, SeekOrigin.Begin);
                return result;
            });
        }

        private T Load<T>(Type baseType, string key, string[] fileTypes, Dictionary<string, T> cache, Func<Stream, T> callback)
        {
            if (baseType == null)
                throw new ArgumentNullException();

            if (string.IsNullOrEmpty(key))
                return default(T);

            string fullkey = string.Format("{0}.{1}", baseType.Namespace, key);

            string basefolder = baseType.Namespace!.Replace('.', Path.DirectorySeparatorChar);

            // Cache fragen
            T result = default(T);
            if (cache != null && cache.TryGetValue(fullkey, out result))
                return result;

            // Versuche Datei zu laden
            foreach (var resourcePack in activePacks)
            {
                string localFolder = Path.Combine(resourcePack.Path, basefolder);

                foreach (var fileType in fileTypes)
                {
                    string filename = Path.Combine(localFolder, string.Format("{0}.{1}", key, fileType));
                    if (File.Exists(filename))
                    {
                        using (var stream = File.Open(filename, FileMode.Open))
                        {
                            result = callback(stream);
                        }
                        break;
                    }
                }

                if (result != null)
                    break;
            }

            // Resource Fallback
            if (result == null)
            {
                var assemblyName = baseType.Assembly.GetName().Name!;

                // Spezialfall Client
                if (assemblyName.Equals("OctoClient"))
                    assemblyName = "OctoAwesome.Client";

                var resKey = fullkey.Replace(assemblyName, string.Format("{0}.Assets", assemblyName));
                foreach (var fileType in fileTypes)
                {
                    using (var stream = baseType.Assembly.GetManifestResourceStream(string.Format("{0}.{1}", resKey, fileType)))
                    {
                        if (stream != null)
                        {
                            result = callback(stream);
                            break;
                        }
                    }
                }
            }

            if (result == null)
            {
                // Im worstcase CheckerTex laden
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("OctoAwesome.Client.Assets.FallbackTexture.png"))
                {
                    result = callback(stream);
                }
            }

            // In Cache speichern
            if (result != null && cache != null)
                cache[fullkey] = result;

            return result;
        }
    }
}
