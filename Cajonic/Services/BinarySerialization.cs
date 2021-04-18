using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Cajonic.Services
{
    public static class BinarySerialization
    {
        public static void WriteToBinaryFile<T>(string filePath, T objectToWrite, bool append = false)
        {
            using (Stream stream = File.Open(filePath, append ? FileMode.Append : FileMode.Create)) {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(stream, objectToWrite);
            }
        }

        public static T ReadFromBinaryFile<T>(string filePath)
        {
            using Stream stream = File.Open(filePath, FileMode.Open);
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            return (T)binaryFormatter.Deserialize(stream);
        }

        //Special serializer for Song which also makes it so the artwork is good to go from byte array
    }
}
