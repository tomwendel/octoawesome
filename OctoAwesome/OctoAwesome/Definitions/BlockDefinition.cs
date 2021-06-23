using System.Drawing;
using engenious;
using System.Collections.Generic;
using OctoAwesome.Information;
using OctoAwesome.Services;
using System;

namespace OctoAwesome.Definitions
{
    /// <summary>
    /// Eine definition eines Block-Typen
    /// </summary>
    public abstract class BlockDefinition : IBlockDefinition
    {
        public virtual uint SolidWall => 0x3f;

        /// <summary>
        /// Der Name des Block-Typen
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Icon für die Toolbar
        /// </summary>
        public abstract string Icon { get; }

        /// <summary>
        /// Die maximale Stackgrösse
        /// </summary>
        public virtual int StackLimit => 100;

        /// <summary>
        /// Gibt das Volumen für eine Einheit an.
        /// </summary>
        public virtual int VolumePerUnit => 125;

        public virtual int VolumePerHit => 25;

        /// <summary>
        /// Array, das alle Texturen für alle Seiten des Blocks enthält
        /// </summary>
        public abstract string[] Textures { get; }

        /// <summary>
        /// Zeigt, ob der Block-Typ Metadaten besitzt
        /// </summary>
        public virtual bool HasMetaData => false;

        public virtual TimeSpan TimeToVolumeReset { get; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Provides the material assigned to this block 
        /// which contains physical properties e.g. hardness, density
        /// </summary>
        public abstract IMaterialDefinition Material { get; }

        private readonly BoundingBox[] defaultCollisionBoxes = new[] { new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1)) };

        /// <summary>
        /// Geplante Methode, mit der der Block auf Interaktion von aussen reagieren kann.
        /// </summary>
        /// <param name="blockVolume">Der Block-Typ des interagierenden Elements</param>
        /// <param name="item">Die physikalischen Parameter des interagierenden Elements</param>
        public virtual BlockHitInformation Hit(BlockVolumeState blockVolume, IItem item)
        {
            //item.Definition.Hit(item, volumeState.BlockDefinition, blockHitInformation);
            var valueMined = item.Hit(Material, blockVolume.BlockInfo, blockVolume.VolumeRemaining, VolumePerHit);
            return new BlockHitInformation(valueMined != 0, valueMined, new[] { (VolumePerUnit, (IDefinition)this) });
        }

        /// <summary>
        /// Liefert die Kollisionsbox für den Block. Da ein Array zurück gegeben wird, lässt sich die 
        /// </summary>
        /// <param name="manager">[Bitte ergänzen]</param>
        /// <param name="x">X-Anteil der Koordinate des Blocks</param>
        /// <param name="y">Y-Anteil der Koordinate des Blocks</param>
        /// <param name="z">Z-Anteil der Koordinate des Blocks</param>
        /// <returns>Ein Array von Kollisionsboxen</returns>
        public virtual BoundingBox[] GetCollisionBoxes(ILocalChunkCache manager, int x, int y, int z)
            => defaultCollisionBoxes;

        public virtual int GetTextureIndex(Wall wall, ILocalChunkCache manager, int x, int y, int z) => 0;

        public virtual int GetTextureRotation(Wall wall, ILocalChunkCache manager, int x, int y, int z) => 0;

        public bool IsSolidWall(Wall wall) => (SolidWall & (1 << (int)wall)) != 0;

    }
}
