﻿using OctoAwesome.Serialization;
using System.IO;
using System.Runtime.CompilerServices;

namespace OctoAwesome
{
    /// <summary>
    /// Base Class for all Components.
    /// </summary>
    public abstract class Component : ISerializable
    {
        public bool Enabled { get; set; }
        public bool Sendable { get; set; }

        public Component()
        {
            Enabled = true;
            Sendable = false;
        }

        /// <summary>
        /// Serialisiert die Entität mit dem angegebenen BinaryWriter.
        /// </summary>
        /// <param name="writer">Der BinaryWriter, mit dem geschrieben wird.</param>
        public virtual void Serialize(BinaryWriter writer)
        {
            writer.Write(Enabled); 
        }

        /// <summary>
        /// Deserialisiert die Entität aus dem angegebenen BinaryReader.
        /// </summary>
        /// <param name="reader">Der BinaryWriter, mit dem gelesen wird.</param>
        public virtual void Deserialize(BinaryReader reader)
        {
            Enabled = reader.ReadBoolean();
        }

        protected virtual void OnPropertyChanged<T>(T value, string callerName)
        {

        }

        protected void SetValue<T>(ref T field, T value, [CallerMemberName]string callerName = "")
        {
            if (field != null)
            {
                if (field.Equals(value))
                    return;
            }

            field = value;

            OnPropertyChanged(field, callerName);
        }
    }
}
