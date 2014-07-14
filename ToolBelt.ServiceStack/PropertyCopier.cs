using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Collections;
using System.Globalization;

namespace ToolBelt.ServiceStack
{
    public static class PropertyCopierExtensions
    {
        public static T CopyAsNew<T>(this object @from) where T: new()
        {
            return PropertyCopier.CopyAsNew<T>(@from);
        }
    }

    public delegate object CopyPropertyDelegate(Type fromType, object fromData, Type toType, Func<PropertyInfo, bool> propInfoPredicate);
    public delegate object CopyListItemDelegate(object fromData);
    public delegate object ChangeTypeDelegate(object fromValue, Type toType);

    public static class PropertyCopier
    {
        private class PropertyMapping
        {
            public PropertyInfo FromPropInfo { get; set; }

            public PropertyInfo ToPropInfo { get; set; }

            public CopyPropertyDelegate CopyProperty { get; set; }

            public bool ConvertNullToDefaultValue { get; set; }
        }

        private static Dictionary<Tuple<Type, Type>, List<PropertyMapping>> typePropMappings = new Dictionary<Tuple<Type, Type>, List<PropertyMapping>>();
        private static Dictionary<Tuple<Type, Type>, ChangeTypeDelegate> typeConverters = new Dictionary<Tuple<Type, Type>, ChangeTypeDelegate>();

        public static T CopyAsNew<T>(object fromObj) where T : new()
        {
            T toObj = new T();

            Copy(fromObj, toObj);

            return toObj;
        }

        public static object CopyAsNew(Type toType, object fromObj)
        {
            object toObj = Activator.CreateInstance(toType);

            Copy(fromObj, toObj);

            return toObj;
        }

        public static void Copy(object fromObj, object toObj, Func<PropertyInfo, bool> propInfoPredicate = null)
        {
            Type fromType = fromObj.GetType();
            Type toType = toObj.GetType();

            if (IsArrayOrList(fromType))
            {
                CopyListItems(fromType, (IList)fromObj, toType, (IList)toObj, propInfoPredicate);
            }
            else
            {
                CopyObject(fromType, fromObj, toType, toObj, propInfoPredicate);
            }
        }

        private static bool IsArrayOrList(Type fromType)
        {
            TypeInfo fromTypeInfo = fromType.GetTypeInfo();

            return
                fromType.IsArray || 
                fromTypeInfo.ImplementedInterfaces.Contains(typeof(IList)) || 
                fromTypeInfo.ImplementedInterfaces.Any(i => 
                { var typeInfo = i.GetTypeInfo(); return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(IList<>); });
        }

        // This is needed for value types where there is no suitable IConvertible method on the 
        // source type.  This happens frequently for UDTs from two unrelated class libraries, 
        // e.g. Rql and MongoDB
        public static void AddTypeConverter(Type fromType, Type toType, ChangeTypeDelegate changeType)
        {
            typeConverters.Add(new Tuple<Type, Type>(fromType, toType), changeType);
        }

        public static List<PropertyInfo> GetProperties(Type type, bool writeable = false)
        {
            var propInfos = type.GetRuntimeProperties();
            List<PropertyInfo> actualPropInfos = new List<PropertyInfo>();

            foreach (var propInfo in propInfos)
            {
                if (!(propInfo.CanRead && propInfo.GetMethod.IsPublic))
                    continue;

                if (writeable && !(propInfo.CanWrite && propInfo.SetMethod.IsPublic))
                    continue; 

                actualPropInfos.Add(propInfo);
            }

            return actualPropInfos;
        }

        private static bool HasDuplicateNames(IEnumerable<PropertyInfo> propInfo)
        {
            IEnumerable<string> names = propInfo.Select(pi => pi.Name);
            var @set = new HashSet<string>();

            foreach (var name in names)
            {
                if (!@set.Add(name))
                    return true;
            }

            return false;
        }

        private static List<PropertyMapping> GetPropertyMappings(Type fromType, Type toType)
        {
            List<PropertyMapping> propMappings;

            lock (typePropMappings)
            {
                if (typePropMappings.TryGetValue(new Tuple<Type,Type>(fromType, toType), out propMappings))
                    return propMappings;

                propMappings = new List<PropertyMapping>();

                List<PropertyInfo> fromPropInfos = GetProperties(fromType);

                if (HasDuplicateNames(fromPropInfos))
                    throw new InvalidOperationException(String.Format("Source type '{0}' contains overloaded properties", fromType.FullName));

                List<PropertyInfo> propInfos = GetProperties(toType, writeable: true);

                if (HasDuplicateNames(propInfos))
                    throw new InvalidOperationException(String.Format("Target type '{0}' contains overloaded properties", toType.FullName));

                Dictionary<string, PropertyInfo> toPropInfos = propInfos.ToDictionary(p => p.Name);

                foreach (var fromPropInfo in fromPropInfos)
                {
                    PropertyInfo toPropInfo;

                    if (!toPropInfos.TryGetValue(fromPropInfo.Name, out toPropInfo))
                        // A writeable, same name property does not exist in the target class; ignore it
                        continue;

                    Type toPropType = toPropInfo.PropertyType;
                    Type fromPropType = fromPropInfo.PropertyType;
                    var copyProperty = (toPropType != fromPropType) ? 
                        GetPropertyCopier(fromPropInfo, toPropInfo) : null;
                    var propMapping = new PropertyMapping();

                    propMapping.FromPropInfo = fromPropInfo;
                    propMapping.ToPropInfo = toPropInfo;
                    propMapping.CopyProperty = copyProperty;
                    // If the target is a value type that is not Nullable<> automatically put in the default value
                    propMapping.ConvertNullToDefaultValue = IsNonNullableValueType(toPropType);

                    propMappings.Add(propMapping);
                }

                typePropMappings.Add(new Tuple<Type,Type>(fromType, toType), propMappings);
            }

            return propMappings;
        }

        static bool IsNonNullableValueType(Type type)
        {
            TypeInfo typeInfo = type.GetTypeInfo();

            return typeInfo.IsValueType && !(typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        static bool IsClass(Type type)
        {
            return type.GetTypeInfo().IsClass;
        }

        static bool IsValueType(Type type)
        {
            return type.GetTypeInfo().IsValueType;
        }

        static bool IsEnumType(Type type)
        {
            return type.GetTypeInfo().IsEnum;
        }

        static MethodInfo GetStaticParseMethod(Type type)
        {
            TypeInfo typeInfo = type.GetTypeInfo();
            var methodInfos = typeInfo.GetDeclaredMethods("Parse");

            return methodInfos.FirstOrDefault(m => 
            {
                if (m.IsPublic && m.IsStatic)
                {
                    var p = m.GetParameters();

                    return (p.Length == 1 && p[0].ParameterType == typeof(String));
                }

                return false;
            });
        }

        public static CopyPropertyDelegate GetPropertyCopier(PropertyInfo fromPropInfo, PropertyInfo toPropInfo)
        {
            Type toPropType = toPropInfo.PropertyType;
            Type fromPropType = fromPropInfo.PropertyType;
            CopyPropertyDelegate copyProperty = null;

            if (toPropType == typeof(string))
            {
                copyProperty = (type, value, otherType, propInfoPredicate) => value.ToString();
            }
            else if (IsEnumType(toPropType))
            {
                copyProperty = (type, value, otherType, propInfoPredicate) => Enum.ToObject(type, value);
            }
            else if (IsValueType(toPropType))
            {
                if (IsValueType(fromPropType))
                {
                    copyProperty = (type, value, otherType, propInfoPredicate) => Convert.ChangeType(value, otherType);
                }
                else if (fromPropType == typeof(string))
                {
                    MethodInfo staticParseMethod = GetStaticParseMethod(toPropType);

                    if (staticParseMethod == null)
                        throw new InvalidCastException("Target type needs static T Parse(string) method");

                    copyProperty = (type, value, otherType, propInfoPredicate) => staticParseMethod.Invoke(null, new object[] { (string)value });
                }
                else
                    throw new InvalidCastException("Can only map value type or string");
            }
            else if (IsArrayOrList(fromPropType))
            {
                if (!IsArrayOrList(toPropType))
                    throw new InvalidCastException("Can only map T[], IList or IList<T> to T[], IList or IList<T>");

                if (toPropType.IsArray)
                {
                    copyProperty = (type, value, otherType, propInfoPredicate) =>
                    {
                        // Create a new instance of the to array and copy over the from array/list
                        object otherValue = Array.CreateInstance(otherType.GetElementType(), ((IList)value).Count);
                        CopyListItems(type, (IList)value, otherType, (IList)otherValue, propInfoPredicate);
                        return otherValue;
                    };
                }
                else
                {
                    copyProperty = (type, value, otherType, propInfoPredicate) =>
                    {
                        // Create a new instance of the to list and copy over the from list
                        object otherValue = Activator.CreateInstance(otherType);
                        CopyListItems(type, (IList)value, otherType, (IList)otherValue, propInfoPredicate);
                        return otherValue;
                    };
                }
            }
            else if (IsClass(toPropInfo.PropertyType) && IsClass(fromPropInfo.PropertyType))
            {
                copyProperty = (type, value, otherType, propInfoPredicate) =>
                {
                    // Create a new instance of the to class property and recursively copy it
                    object otherValue = Activator.CreateInstance(toPropInfo.PropertyType);
                    CopyObject(type, value, otherType, otherValue, propInfoPredicate);
                    return otherValue;
                };
            }
            else
            {
                // If we can't copy, create a default instance of the other value
                copyProperty = (type, value, otherType, propInfoPredicate) =>
                {
                    return Activator.CreateInstance(otherType);
                };
            }

            return copyProperty;
        }

        public static CopyListItemDelegate GetListItemCopier(Type fromItemType, Type toItemType)
        {
            if (toItemType == fromItemType)
            {
                return (fromValue) => fromValue;
            }
            else if (toItemType == typeof(string))
            {
                return (fromValue) => fromValue.ToString();
            }
            else if (IsEnumType(toItemType))
            {
                return (fromValue) => Enum.ToObject(toItemType, fromValue);
            }
            else if (IsValueType(toItemType) && IsValueType(fromItemType))
            {
                ChangeTypeDelegate changeType;
                
                if (typeConverters.TryGetValue(new Tuple<Type, Type>(fromItemType, toItemType), out changeType))
                    return (fromValue) => changeType(fromValue, toItemType);
                else
                    return (fromValue) => Convert.ChangeType(fromValue, toItemType);
            }
            else
            {
                return (fromValue) => 
                {
                    var toItem = Activator.CreateInstance(toItemType);

                    CopyObject(fromItemType, fromValue, toItemType, toItem, null);

                    return toItem;
                };
            }
        }

        private static void CopyObject(Type fromType, object fromObj, Type toType, object toObj, Func<PropertyInfo, bool> propInfoPredicate)
        {
            List<PropertyMapping> pms = GetPropertyMappings(fromType, toType);

            if (propInfoPredicate != null)
            {
                pms = pms.Where(pm => propInfoPredicate(pm.FromPropInfo)).ToList();
            }

            foreach (var pm in pms)
            {
                object value = pm.FromPropInfo.GetValue(fromObj, null);

                if (value == null)
                {
                    if (pm.ConvertNullToDefaultValue)
                        value = Activator.CreateInstance(pm.ToPropInfo.PropertyType);

                    // Fall through...
                }
                else if (pm.CopyProperty != null)
                {
                    value = pm.CopyProperty(pm.FromPropInfo.PropertyType, value, pm.ToPropInfo.PropertyType, propInfoPredicate);
                }

                pm.ToPropInfo.SetValue(toObj, value, null);
            }
        }

        private static Type GetArrayOrListItemType(Type type)
        {
            if (type.IsArray)
                return type.GetElementType();
            else
            {
                TypeInfo typeInfo = type.GetTypeInfo();

                if (typeInfo.IsGenericType)
                    return typeInfo.GenericTypeArguments[0];
                else
                    return typeof(object);
            }
        }

        private static void CopyListItems(Type fromType, IList fromList, Type toType, IList toList, Func<PropertyInfo, bool> propInfoPredicate)
        {
            Type fromItemType = GetArrayOrListItemType(fromType);
            Type toItemType = GetArrayOrListItemType(toType);

            // TODO: Pull this up higher and store it; once calculated it never changes
            var listItemCopier = GetListItemCopier(fromItemType, toItemType);

            if (toType.IsArray)
            {
                int i = 0;
                Array arr = (Array)toList;

                foreach (var fromValue in fromList)
                {
                    object obj = listItemCopier(fromValue);

                    arr.SetValue(obj, i++);
                }
            }
            else
            {
                foreach (var fromValue in fromList)
                {
                    toList.Add(listItemCopier(fromValue));
                }
            }
        }
    }
}


