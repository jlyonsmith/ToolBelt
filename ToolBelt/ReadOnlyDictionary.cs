using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace ToolBelt
{
    public class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary
    {
        #region Fields
        private IDictionary<TKey, TValue> dictionary;

        #endregion

        #region Construction
        public ReadOnlyDictionary()
        {
            this.dictionary = new ReadOnlyDictionary<TKey, TValue>(); 
        }

        public ReadOnlyDictionary(IDictionary<TKey, TValue> dictionary)
        {
            this.dictionary = dictionary;
        }

        #endregion

        #region IDictionary<TKey,TValue> Members

        public void Add(TKey key, TValue value)
        {
            throw new NotSupportedException("This dictionary is read-only");
        }

        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        public ICollection<TKey> Keys
        {
            get { return dictionary.Keys; }
        }

        public bool Remove(TKey key)
        {
            throw new NotSupportedException("This dictionary is read-only");
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        public ICollection<TValue> Values
        {
            get { return dictionary.Values; }
        }

        public TValue this[TKey key]
        {
            get
            {
                return dictionary[key];
            }
            set
            {
                throw new NotSupportedException("This dictionary is read-only");
            }
        }

        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>> Members

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException("This dictionary is read-only");
        }

        public void Clear()
        {
            throw new NotSupportedException("This dictionary is read-only");
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return dictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            dictionary.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return dictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException("This dictionary is read-only");
        }

        #endregion

        #region IEnumerable<KeyValuePair<TKey,TValue>> Members

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return (dictionary as System.Collections.IEnumerable).GetEnumerator();
        }

        #endregion

        #region IDictionary Members

        public void Add(object key, object value)
        {
            throw new NotSupportedException("This dictionary is read-only");
        }

        public bool Contains(object key)
        {
            return dictionary.ContainsKey((TKey)key);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return (IDictionaryEnumerator)dictionary.GetEnumerator();
        }

        public bool IsFixedSize
        {
            get { return true; }
        }

        ICollection IDictionary.Keys
        {
            get
            {
                return (ICollection)dictionary.Keys;
            }
        }

        public void Remove(object key)
        {
            throw new NotSupportedException("This dictionary is read-only");
        }

        ICollection IDictionary.Values
        {
            get 
            { 
                return (ICollection)dictionary.Values; 
            }
        }

        public object this[object key]
        {
            get
            {
                return dictionary[(TKey)key];
            }
            set
            {
                throw new NotSupportedException("This dictionary is read-only");
            }
        }

        #endregion

        #region ICollection Members

        public void CopyTo(Array array, int index)
        {
            dictionary.CopyTo((KeyValuePair<TKey, TValue>[])array, index);
        }

        public bool IsSynchronized
        {
            get { return true; }
        }

        public object SyncRoot
        {
            get
            {
                throw new NotSupportedException("This dictionary is read-only");
            }
        }

        #endregion
    }
}
