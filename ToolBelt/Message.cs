using System;
using System.Resources;
using System.Collections.Generic;
using System.Globalization;

namespace ToolBelt
{
#if WINDOWS
    [Serializable]
#endif
    public class Message
    {
        // Fields
        private object[] array;
        private string name;
#if WINDOWS
        [NonSerialized]
#endif
        private ResourceManager rm;
        private Type t;
        private static Dictionary<Type, ResourceManager> resourceManagers = new Dictionary<Type, ResourceManager>();

        // Methods
        private Message()
        {
        }

        public Message(string name, Type type, ResourceManager resourceManager, object[] array)
        {
            this.name = name;
            this.rm = resourceManager;
            this.array = array;
            this.t = type;
        }

        public static implicit operator string(Message utfMessage)
        {
            return utfMessage.ToString();
        }

        public override string ToString()
        {
            string format = this.RM.GetString(this.Name, CultureInfo.CurrentUICulture);
            object[] parameters = this.Params;
            if (parameters != null)
            {
                return string.Format(CultureInfo.CurrentCulture, format, parameters);
            }
            return format;
        }

        // Properties
        internal string Name
        {
            get
            {
                return this.name;
            }
        }

        internal object[] Params
        {
            get
            {
                return this.array;
            }
        }

        public ResourceManager RM
        {
            get
            {
                if ((this.rm == null) && !resourceManagers.TryGetValue(this.t, out this.rm))
                {
                    resourceManagers.Add(this.t, this.rm = new ResourceManager(this.t));
                }
                return this.rm;
            }
        }
    }
}