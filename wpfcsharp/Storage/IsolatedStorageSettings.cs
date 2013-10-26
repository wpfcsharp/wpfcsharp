using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.Serialization;

namespace wpfcsharp.Storage
{
    /// <summary>
    /// Provides a dictionary that stores key-value pairs in isolated storage.
    /// </summary>
    public sealed class IsolatedStorageSettings : IEnumerable, INotifyCollectionChanged
    {
        #region Fields

        private readonly Object syncLock = new Object();
        private const String SettingsFile = "info.dat";
        private readonly IDictionary<String, KeyValuePair<Guid, Object>> dictionary;
        private readonly IsolatedStorageFile storageFile;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the IsolatedStorageSettings class.
        /// </summary>
        static IsolatedStorageSettings()
        {
            if (ApplicationSettings == null) ApplicationSettings = new IsolatedStorageSettings();
        }

        /// <summary>
        /// Initializes a new instance of the IsolatedStorageSettings class.
        /// </summary>
        private IsolatedStorageSettings()
        {
            dictionary = new Dictionary<String, KeyValuePair<Guid, Object>>();
            storageFile = IsolatedStorageFile.GetUserStoreForAssembly();
            Restore();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets an instance of IsolatedStorageSettings.
        /// </summary>
        public static IsolatedStorageSettings ApplicationSettings { get; private set; }

        /// <summary>
        /// Gets the number of key-value pairs that are stored in the dictionary.
        /// </summary>
        public Int32 Count
        {
            get { return dictionary.Count; }
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the item to get.</param>
        /// <returns>The value associated with the specified key.</returns>
        public Object this[String key]
        {
            get
            {
                KeyValuePair<Guid, Object> keyValuePair;
                return dictionary.TryGetValue(key, out keyValuePair) ? keyValuePair.Value : null;
            }
        }

        /// <summary>
        /// Gets a collection that contains the keys in the dictionary.
        /// </summary>
        public IEnumerable<String> Keys
        {
            get { return dictionary.Keys; }
        }

        /// <summary>
        /// Gets a collection that contains the values in the dictionary.
        /// </summary>
        public IEnumerable<Object> Values
        {
            get { return dictionary.Values.Select(keyValuePair => keyValuePair.Value); }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Determines if the application settings dictionary contains the specified key.
        /// </summary>
        /// <param name="key">The key for the entry to be located.</param>
        /// <returns>true if the dictionary contains the specified key; otherwise, false.</returns>
        public Boolean ContainsKey(String key)
        {
            if (key == null) throw new ArgumentNullException("key");
            return dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Gets a value for the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">
        /// When this method returns, the value associated with the specified key if the key is found; otherwise, the default value for the type of the value parameter.
        /// This parameter is passed uninitialized.
        /// </param>
        /// <returns>true if the specified key is found; otherwise, false.</returns>
        public Boolean TryGet(String key, out Object value)
        {
            if (key == null) throw new ArgumentNullException("key");
            KeyValuePair<Guid, Object> pair;
            var result = dictionary.TryGetValue(key, out pair);
            value = pair.Value;
            return result;
        }

        /// <summary>
        /// Adds an entry to the dictionary for the key-value pair.
        /// </summary>
        /// <param name="key">The key for the entry to be stored.</param>
        /// <param name="value">The value to be stored.</param>
        /// <returns>true if the specified key was added; otherwise, false.</returns>
        public Boolean TryAdd(String key, Object value)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");
            if (key.Equals(SettingsFile)) throw new ArgumentException("key");
            if (!value.IsSerializable()) throw new SerializationException("A object could not be serialized.");
            lock (syncLock)
            {
                Boolean result;
                if (dictionary.ContainsKey(key))
                    result = TryUpdateWithNotification(key, value);
                else
                {
                    result = TryAddWithNotification(key, value);
                    if (result) Save();
                }
                return result;
            }
        }

        /// <summary>
        /// Removes the entry with the specified key.
        /// </summary>
        /// <param name="key">The key for the entry to be deleted.</param>
        /// <returns>true if the specified key was removed; otherwise, false.</returns>
        public Boolean TryRemove(String key)
        {
            if (key == null) throw new ArgumentNullException("key");
            lock (syncLock)
            {
                if (!dictionary.ContainsKey(key)) return false;
                var result = TryRemoveWithNotification(key);
                if (result) Save();
                return result;
            }
        }

        /// <summary>
        /// Resets the count of items stored in IsolatedStorageSettings to zero and releases all references to elements in the collection.
        /// </summary>
        public void Clear()
        {
            lock (syncLock)
            {
                TryClearWithNotification();
                Save();
            }
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An IEnumerator object that can be used to iterate through the collection.</returns>
        public IEnumerator GetEnumerator()
        {
            return new SettingsEnumerator(dictionary.Select(item => new KeyValuePair<String, Object>(item.Key, item.Value.Value)).ToArray());
        }

        #endregion

        #region INotifyCollectionChanged Members

        /// <summary>
        /// Occurs when the collection changes.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="args">The event data for the CollectionChanged event.</param>
        private void RaiseCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            // Is anybody out there?
            var handler = CollectionChanged;
            if (handler == null) return;
            // Somebody is listening, so raise a property changed event
            handler(this, args);
        }

        #endregion

        #region Private Members

        /// <summary>
        /// Restore settings.
        /// </summary>
        private void Restore()
        {
            var settings = IsolatedStorageSerializer.BinaryDeserialize(SettingsFile, storageFile) as Dictionary<String, Guid> ?? new Dictionary<String, Guid>();
            foreach (var item in settings)
            {
                var value = IsolatedStorageSerializer.BinaryDeserialize(item.Value.ToString(), storageFile);
                dictionary.Add(item.Key, new KeyValuePair<Guid, Object>(item.Value, value));
            }
        }

        /// <summary>
        /// Saves data written to the current IsolatedStorageSettings object.
        /// </summary>
        private void Save()
        {
            var settings = dictionary.ToDictionary(item => item.Key, item => item.Value.Key);
            IsolatedStorageSerializer.BinarySerialize(settings, SettingsFile, storageFile);
        }

        /// <summary>Attempts to add an item to the dictionary, notifying observers of any changes.</summary>
        /// <param name="key">The key of the item to be added.</param>
        /// <param name="value">The value of the item to be added.</param>
        /// <returns>Whether the add was successful.</returns>
        private Boolean TryAddWithNotification(String key, Object value)
        {
            var guid = Guid.NewGuid();
            var result = IsolatedStorageSerializer.BinarySerialize(value, guid.ToString(), storageFile);
            if (result)
            {
                dictionary.Add(key, new KeyValuePair<Guid, Object>(guid, value));
                RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                                                                            new KeyValuePair<String, Object>(key, value)));
            }
            return result;
        }

        /// <summary>Attempts to remove an item from the dictionary, notifying observers of any changes.</summary>
        /// <param name="key">The key of the item to be removed.</param>
        /// <returns>Whether the removal was successful.</returns>
        private Boolean TryRemoveWithNotification(String key)
        {
            KeyValuePair<Guid, Object> pair;
            var result = dictionary.TryGetValue(key, out pair);
            if (result)
            {
                dictionary.Remove(key);
                try
                {
                    storageFile.DeleteFile(pair.Key.ToString());
                }
                catch (IsolatedStorageException ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                                                                            new KeyValuePair<String, Object>(key, pair.Value)));
            }
            return result;
        }

        /// <summary>Attempts to update an item in the dictionary, notifying observers of any changes.</summary>
        /// <param name="key">The key of the item to be updated.</param>
        /// <param name="value">The new value to set for the item.</param>
        /// <returns>Whether the update was successful.</returns>
        private Boolean TryUpdateWithNotification(String key, Object value)
        {
            if (!value.IsSerializable()) throw new SerializationException("A object could not be serialized.");
            KeyValuePair<Guid, Object> oldPair;
            var result = false;
            if (dictionary.TryGetValue(key, out oldPair))
            {
                result = IsolatedStorageSerializer.BinarySerialize(value, oldPair.Key.ToString(), storageFile);
                var newPair = new KeyValuePair<Guid, Object>(oldPair.Key, value);
                dictionary[key] = newPair;
                RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
                                                                                        new KeyValuePair<String, Object>(key, newPair.Value),
                                                                                        new KeyValuePair<String, Object>(key, oldPair.Value)));
            }
            return result;
        }

        /// <summary>
        /// Attempts to reset the dictionary, notifying observers of any changes.
        /// </summary>
        private void TryClearWithNotification()
        {
            foreach (var item in dictionary)
            {
                try
                {
                    storageFile.DeleteFile(item.Value.Key.ToString());
                }
                catch (IsolatedStorageException ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
            dictionary.Clear();
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        #endregion
    }
}
