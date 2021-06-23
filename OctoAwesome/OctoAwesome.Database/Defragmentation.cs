using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OctoAwesome.Database
{
    public sealed class Defragmentation<TTag> where TTag : ITag, new()
    {
        private readonly FileInfo keyStoreFile;
        private readonly FileInfo valueStoreFile;

        public Defragmentation(FileInfo keyStoreFile, FileInfo valueStoreFile)
        {
            this.keyStoreFile = keyStoreFile;
            this.valueStoreFile = valueStoreFile;
        }

        public void StartDefragmentation()
        {
            var newValueStoreFile = new FileInfo(Path.GetTempFileName());
            var keyBuffer = new byte[Key<TTag>.KEY_SIZE];

            IEnumerable<Key<TTag>> keys = DefragmentValues(newValueStoreFile, keyBuffer);

            keyStoreFile.Delete();
            WriteKeyFile(keys);

            valueStoreFile.Delete();
            newValueStoreFile.MoveTo(valueStoreFile.FullName);
        }

        public void RecreateKeyFile()
        {
            var keyBuffer = new byte[Key<TTag>.KEY_SIZE];

            IEnumerable<Key<TTag>> keys = GetKeys(keyBuffer);

            keyStoreFile.Delete();
            WriteKeyFile(keys);
        }

        private void WriteKeyFile(IEnumerable<Key<TTag>> keyList)
        {
            using (FileStream newKeyStoreFile = keyStoreFile.Open(FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {

                foreach (Key<TTag> key in keyList)
                    newKeyStoreFile.Write(key.GetBytes(), 0, Key<TTag>.KEY_SIZE);
            }
        }

        private IEnumerable<Key<TTag>> DefragmentValues(FileInfo newValueStoreFile, byte[] keyBuffer)
        {
            using (FileStream newValueStoreStream = newValueStoreFile.Open(FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            using (FileStream currentValueStoreStream = valueStoreFile.Open(FileMode.Open, FileAccess.Read, FileShare.None))
            {
                do
                {
                    if (currentValueStoreStream.Read(keyBuffer, 0, keyBuffer.Length) == 0)
                        break;
                    var key = Key<TTag>.FromBytes(keyBuffer, 0);

                    if (key.IsEmpty)
                    {
                        Span<byte> intBuffer = stackalloc byte[sizeof(int)];
                        if (currentValueStoreStream.Read(intBuffer) == 0)
                            break;
                        var length = BitConverter.ToInt32(intBuffer) - sizeof(int);
                        if (length < 0)
                            throw new DataMisalignedException();
                        currentValueStoreStream.Seek(length, SeekOrigin.Current);
                    }
                    else
                    {
                        Span<byte> buffer = stackalloc byte[key.ValueLength];
                        if (currentValueStoreStream.Read(buffer) == 0)
                            break;
                        newValueStoreStream.Write(keyBuffer, 0, keyBuffer.Length);
                        newValueStoreStream.Write(buffer);
                        yield return key;
                    }
                } while (currentValueStoreStream.Position < currentValueStoreStream.Length);
            }
        }

        private IEnumerable<Key<TTag>> GetKeys(byte[] keyBuffer)
        {
            using (FileStream fileStream = valueStoreFile.Open(FileMode.Open, FileAccess.Read, FileShare.None))
            {
                do
                {
                    if (fileStream.Read(keyBuffer, 0, keyBuffer.Length) == 0)
                        break;
                    var key = Key<TTag>.FromBytes(keyBuffer, 0);
                    long length = 0;

                    if (key.IsEmpty)
                    {
                        Span<byte> intBuffer = stackalloc byte[sizeof(int)];
                        if (fileStream.Read(intBuffer) == 0)
                            break;
                        length = BitConverter.ToInt32(intBuffer) - sizeof(int);
                    }
                    else
                    {
                        length = key.ValueLength;
                    }
                    if (length < 0)
                        throw new DataMisalignedException();
                    fileStream.Seek(length, SeekOrigin.Current);
                    yield return key;
                } while (fileStream.Position < fileStream.Length);
            }
        }
    }
}
