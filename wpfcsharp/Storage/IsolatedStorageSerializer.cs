using System;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace wpfcsharp.Storage
{
    /// <summary>
    /// Сериализует и десериализует объекты в документы XML.
    /// </summary>
    /// <typeparam name="T">Тип объекта для сериализации или десериализации.</typeparam>
    internal static class IsolatedStorageSerializer<T> where T : class
    {
        #region Public Methods

        /// <summary>
        /// Сериализует указанный объект в файл.
        /// </summary>
        /// <param name="instance">Объект для сериализации.</param>
        /// <param name="path">Путь к файлу.</param>
        /// <param name="storageFile">Область изолированного хранения, содержащая файл.</param>
        /// <returns>True - объект сериализован, иначе - False.</returns>
        public static Boolean BinarySerialize(T instance, String path, IsolatedStorageFile storageFile)
        {
            // Проверка объектов
            if (string.IsNullOrEmpty(path)) return false;
            var result = false;
            // Подготовка потоков
            using (var fileStream = new IsolatedStorageFileStream(path, FileMode.OpenOrCreate, storageFile))
            {
                // Десериализуем объект
                var binaryFormatter = new BinaryFormatter();
                try
                {
                    binaryFormatter.Serialize(fileStream, instance);
                    result = true;
                }
                catch (Exception ex)
                {
                    // Возникла ошибка при десериализации
                    Debug.WriteLine(ex);
                }
            }
            return result;
        }

        /// <summary>
        /// Десериализует указанный сериализованный ранее объект.
        /// </summary>
        /// <param name="path">Путь к файлу.</param>
        /// <param name="storageFile">Область изолированного хранения, содержащая файл.</param>
        /// <returns>Десериализованный объект или null в случае неудачи.</returns>
        public static T BinaryDeserialize(String path, IsolatedStorageFile storageFile)
        {
            // Проверка объектов
            if (string.IsNullOrEmpty(path)) return null;
            T instance = null;
            // Подготовка потоков
            using (var fileStream = new IsolatedStorageFileStream(path, FileMode.OpenOrCreate, storageFile))
            {
                // Десериализуем объект
                var binaryFormatter = new BinaryFormatter();
                try
                {
                    instance = (T)binaryFormatter.Deserialize(fileStream);
                }
                catch (Exception ex)
                {
                    // Возникла ошибка при десериализации
                    Debug.WriteLine(ex);
                }
            }
            return instance;
        }

        #endregion
    }

    /// <summary>
    /// Сериализует и десериализует объекты в документы XML.
    /// </summary>
    internal static class IsolatedStorageSerializer
    {
        #region Public Methods

        /// <summary>
        /// Сериализует указанный объект в файл.
        /// </summary>
        /// <param name="instance">Объект для сериализации.</param>
        /// <param name="path">Путь к файлу.</param>
        /// <param name="storageFile">Область изолированного хранения, содержащая файл.</param>
        /// <returns>True - объект сериализован, иначе - False.</returns>
        public static Boolean BinarySerialize(Object instance, String path, IsolatedStorageFile storageFile)
        {
            // Проверка объектов
            if (string.IsNullOrEmpty(path)) return false;
            var result = false;
            // Подготовка потоков
            using (var fileStream = new IsolatedStorageFileStream(path, FileMode.OpenOrCreate, storageFile))
            {
                // Десериализуем объект
                var binaryFormatter = new BinaryFormatter();
                try
                {
                    binaryFormatter.Serialize(fileStream, instance);
                    result = true;
                }
                catch (Exception ex)
                {
                    // Возникла ошибка при десериализации
                    Debug.WriteLine(ex);
                }
            }
            return result;
        }

        /// <summary>
        /// Десериализует указанный сериализованный ранее объект.
        /// </summary>
        /// <param name="path">Путь к файлу.</param>
        /// <param name="storageFile">Область изолированного хранения, содержащая файл.</param>
        /// <returns>Десериализованный объект или null в случае неудачи.</returns>
        public static Object BinaryDeserialize(String path, IsolatedStorageFile storageFile)
        {
            // Проверка объектов
            if (string.IsNullOrEmpty(path)) return null;
            Object instance = null;
            // Подготовка потоков
            using (var fileStream = new IsolatedStorageFileStream(path, FileMode.OpenOrCreate, storageFile))
            {
                // Десериализуем объект
                var binaryFormatter = new BinaryFormatter();
                try
                {
                    instance = binaryFormatter.Deserialize(fileStream);
                }
                catch (Exception ex)
                {
                    // Возникла ошибка при десериализации
                    Debug.WriteLine(ex);
                }
            }
            return instance;
        }

        #endregion

        #region Helper Members

        /// <summary>
        /// Определяет, возможно ли сериализовать объект.
        /// </summary>
        /// <param name="value">Объект, который требуется сериализовать.</param>
        /// <returns>true - объект сериализуем, иначе - false.</returns>
        public static Boolean IsSerializable(this Object value)
        {
            if (value is ISerializable || value.GetType().IsSerializable) return true;
            return Attribute.IsDefined(value.GetType(), typeof(SerializableAttribute));
        }

        #endregion
    }
}
