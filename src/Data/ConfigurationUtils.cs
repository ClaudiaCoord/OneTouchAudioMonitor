/*
 * Git: https://github.com/ClaudiaCoord/OneTouchAudioMonitor
 * Copyright (c) 2022 СС
 * License MIT.
*/
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;

namespace OneTouchMonitor.Data
{
    public static class ConfigurationUtils
    {
        #region Xml
        /// <summary>
        /// Сохранение данных в файл
        /// </summary>
        /// <typeparam name="T1">тип данных</typeparam>
        /// <param name="path">путь к файлу <see cref="string">string</see></param>
        /// <param name="src">данные</param>
        /// <param name="isappend">перезаписывать файл</param>
        /// <param name="enc"><see cref="Encoding">Encoding</see></param>
        public static async Task SerializeToFile<T1>(this string path, T1 src, bool isappend = false)
        {
            if (src == null) return;
            using MemoryStream stream = new();
            XmlSerializer xml = new(typeof(T1));
            xml.Serialize(stream, src);
            StorageFolder folder = await GetFolder().ConfigureAwait(false);
            StorageFile file = await folder.CreateFileAsync(
                path,
                isappend ? CreationCollisionOption.OpenIfExists : CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteBytesAsync(file, stream.ToArray());
        }
        /// <summary>
        /// Чтение данных из файла
        /// </summary>
        /// <typeparam name="T1">тип данных</typeparam>
        /// <param name="path">путь к файлу <see cref="string">string</see></param>
        /// <param name="enc"><see cref="Encoding">Encoding</see></param>
        /// <returns>согласно типу данных</returns>
        public static async Task<T1> DeserializeFromFile<T1>(this string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return default;
            StorageFolder folder = await GetFolder().ConfigureAwait(false);
            using Stream stream = await folder.OpenStreamForReadAsync(path);
            if (stream == null) return default;
            XmlSerializer xml = new(typeof(T1));
            if (xml.Deserialize(stream) is T1 val)
                return val;
            return default;
        }

        private static async Task<StorageFolder> GetFolder() =>
            await KnownFolders.GetFolderForUserAsync(null, KnownFolderId.CurrentAppMods);
        #endregion
    }
}
