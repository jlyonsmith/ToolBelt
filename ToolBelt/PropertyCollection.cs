using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections;
using System.IO;
using System.Linq;
using ToolBelt;

namespace Playroom
{
    public class PropertyCollection : IDictionary<string, string>, IDictionary
    {
        #region Private Fields
        private Dictionary<string, string> dictionary;

        #endregion

        #region Properties
        private Dictionary<string, string> Dictionary
        {
            get
            {
                if (dictionary == null)
                    dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                return dictionary;
            }
        }

        public ReadOnlyDictionary<string, string> AsReadOnlyDictionary()
        {
            return new ReadOnlyDictionary<string, string>(this.Dictionary);
        }

        #endregion

        #region Public Events
        public event EventHandler<PropertiesChangedEventArgs> Changed;
        
        #endregion        

        #region Public Methods
        public void AddFromEnvironment()
        {
            IDictionary entries = Environment.GetEnvironmentVariables();

            foreach (DictionaryEntry entry in entries)
            {
                if (!String.IsNullOrEmpty((string)entry.Key))
                    this[entry.Key] = entry.Value;
            }
        }

        public void AddFromPropertyString(string keyValuePairString)
        {
            if (String.IsNullOrEmpty(keyValuePairString))
                return;

            string[] keyValuePairs = keyValuePairString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string keyValuePair in keyValuePairs)
            {
                string[] keyAndValue = keyValuePair.Split('=');

                if (keyAndValue.Length == 2)
                {
                    this[keyAndValue[0]] = keyAndValue[1];
                }
            }
        }

        #endregion

        #region Private Methods
        private void OnAddComplete(string key, string value)
        {
            EventHandler<PropertiesChangedEventArgs> temp = Changed;

            if (temp != null)
            {
                temp(this, new PropertiesChangedEventArgs(ChangeType.Added, key, value, null));
            }
        }

        private void OnSetComplete(string key, string oldValue, string newValue)
        {
            EventHandler<PropertiesChangedEventArgs> temp = Changed;

            if (temp != null)
            {
                temp(this, new PropertiesChangedEventArgs(ChangeType.Replaced, key, oldValue, newValue));
            }
        }

        private void OnRemoveComplete(string key, string value)
        {
            EventHandler<PropertiesChangedEventArgs> temp = Changed;

            if (temp != null)
            {
                temp(this, new PropertiesChangedEventArgs(ChangeType.Removed, key, value, null));
            }
        }

        private void OnClearComplete()
        {
            EventHandler<PropertiesChangedEventArgs> temp = Changed;

            if (temp != null)
            {
                temp(this, new PropertiesChangedEventArgs(ChangeType.Cleared, null, null, null));
            }
        }

        #endregion

        #region IDictionary<string,string> Members

        public bool ContainsKey(string key)
        {
            return this.Dictionary.ContainsKey(key);
        }

        public ICollection<string> Keys
        {
            get { return this.Dictionary.Keys; }
        }

        public bool Remove(string key)
        {
            string value;

            if (this.Dictionary.TryGetValue(key, out value))
            {
                this.Dictionary.Remove(key);
                try
                {
                    OnRemoveComplete(key, value);
                }
                catch
                {
                    this.Dictionary.Add(key, value);
                    return false;
                }
            }

            return true;
        }

        bool IDictionary<string, string>.TryGetValue(string key, out string value)
        {
            return this.Dictionary.TryGetValue(key, out value);
        }

        public ICollection<string> Values
        {
            get { return this.Dictionary.Values; }
        }

        void IDictionary<string, string>.Add(string key, string value)
        {
            this.Dictionary.Add(key, value);

            try
            {
                OnAddComplete(key, value);
            }
            catch
            {
                this.Dictionary.Remove(key);
            }
        }

        public string this[string key]
        {
            get
            {
                string value;

                return (this.Dictionary.TryGetValue(key, out value) ? value : null);
            }
            set
            {
                string oldValue = null;
                bool hadOldValue = this.Dictionary.TryGetValue(key, out oldValue);

                this.Dictionary[key] = value;

                try
                {
                    OnSetComplete(key, oldValue, value);
                }
                catch
                {
                    if (hadOldValue)
                        this.Dictionary[key] = oldValue;
                    else
                        this.Dictionary.Remove(key);
                }
            }
        }

        #endregion

        #region ICollection<KeyValuePair<string,string>> Members

        public void Add(KeyValuePair<string, string> item)
        {
            ((IDictionary<string, string>)this).Add(item.Key, item.Value);
        }

        public void Clear()
        {
            this.Dictionary.Clear();
            OnClearComplete();
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, string>>)this.Dictionary).CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return this.Dictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return ((ICollection<KeyValuePair<string, string>>)this.Dictionary).IsReadOnly; }
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            return Remove(item.Key);
        }

        #endregion

        #region IEnumerable<KeyValuePair<string,string>> Members

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return this.Dictionary.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Dictionary.GetEnumerator();
        }

        #endregion

        #region IDictionary Members

        public void Add(object key, object value)
        {
            ((IDictionary<string, string>)this).Add((string)key, (string)value);
        }

        public bool Contains(object key)
        {
            return ContainsKey((string)key);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return this.Dictionary.GetEnumerator();
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        ICollection IDictionary.Keys
        {
            get { return this.Dictionary.Keys; }
        }

        public void Remove(object key)
        {
            this.Dictionary.Remove((string)key);
        }

        ICollection IDictionary.Values
        {
            get { return this.Dictionary.Values; }
        }

        public object this[object key]
        {
            get
            {
                return ((IDictionary<string, string>)this)[(string)key];
            }
            set
            {
                ((IDictionary<string, string>)this)[(string)key] = (string)value;
            }
        }

        #endregion

        #region ICollection Members

        public void CopyTo(Array array, int index)
        {
            ((IDictionary)this.Dictionary).CopyTo(array, index);
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { throw new NotImplementedException(); }
        }

        #endregion
    }

    public class PropertiesChangedEventArgs : EventArgs
    {
        public string Key { get;  private set; }
        public string Value { get; private set; }
        public string NewValue { get; private set; }
        public ChangeType ChangeType { get; internal set; }

        public PropertiesChangedEventArgs(ChangeType changeType, object key, object value, object newValue)
        {
            ChangeType = changeType;
            Key = (string)key;
            Value = (string)value;
            NewValue = (string)newValue;
        }
    }

    public enum ChangeType
    {
        Added, 
        Removed, 
        Replaced, 
        Cleared
    };
}
