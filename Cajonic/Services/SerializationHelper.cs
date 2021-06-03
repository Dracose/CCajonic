using System.IO;
using System.Threading.Tasks;
using ProtoBuf;

namespace Cajonic.Services
{
    public static class SerializationHelper
    {
        public static async Task WriteToBinaryFile<T>(string filePath, T objectToWrite)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            await using FileStream file = File.Create(filePath);
            Serializer.Serialize(file, objectToWrite);
        }

        public static async Task<T> ReadFromBinaryFile<T>(string filePath)
        {
            await using Stream stream = File.Open(filePath, FileMode.Open);
            return (T)Serializer.Deserialize<T>(stream);
        }

        public static Task DestroyBinaryFile(string filePath)
        {
            return File.Exists(filePath) ? Task.Run(() => File.Delete(filePath)) : Task.CompletedTask;
        }
    }
}
