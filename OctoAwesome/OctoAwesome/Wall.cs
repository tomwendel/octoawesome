using System;

namespace OctoAwesome
{
    public enum Wall
    {
        /// <summary>
        /// Top +Z
        /// </summary>
        Top,
        /// <summary>
        /// Bottom -Z
        /// </summary>
        Bottom,
        /// <summary>
        /// West -X
        /// </summary>
        Left,
        /// <summary>
        /// East +X
        /// </summary>
        Right,
        /// <summary>
        /// North +Y
        /// </summary>
        Back,
        /// <summary>
        /// South -Y, 
        /// </summary>
        Front
    }
}