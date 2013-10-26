using System;
using System.Collections;
using System.Collections.Generic;

namespace wpfcsharp.Storage
{
    /// <summary>
    /// Supports a simple iteration over a non-generic collection.
    /// </summary>
    public class SettingsEnumerator : IEnumerator
    {
        #region Fields

        private readonly KeyValuePair<String, Object>[] array;
        int position = -1;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the SettingsEnumerator class.
        /// </summary>
        /// <param name="array">Array of object.</param>
        public SettingsEnumerator(KeyValuePair<String, Object>[] array)
        {
            this.array = array;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        public KeyValuePair<String, Object> Current
        {
            get
            {
                try
                {
                    return array[position];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        #endregion

        #region IEnumerator Members

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
        public bool MoveNext()
        {
            position++;
            return (position < array.Length);
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        public void Reset()
        {
            position = -1;
        }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        object IEnumerator.Current
        {
            get { return Current; }
        }

        #endregion
    }
}
