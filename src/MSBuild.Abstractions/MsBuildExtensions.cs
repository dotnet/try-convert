using Microsoft.Build.Construction;

using System;
using System.Reflection;
using System.Xml;

namespace MSBuild.Abstractions
{
    public static class MsBuildExtensions
    {
        public static XmlElement GetXml(this ProjectItemElement projectItem)
        {
            return (XmlElement)GetPropertyValue(projectItem, "XmlElement");

            static object GetPropertyValue(object obj, string propertyName)
            {
                if (obj == null)
                {
                    throw new ArgumentNullException("obj");
                }

                var objType = obj.GetType();
                var propInfo = GetPropertyInfo(objType, propertyName);
                if (propInfo == null)
                {
                    throw new ArgumentOutOfRangeException("propertyName",
                      string.Format("Couldn't find property {0} in type {1}", propertyName, objType.FullName));
                }

                return propInfo.GetValue(obj, null);
            }

            static PropertyInfo GetPropertyInfo(Type type, string propertyName)
            {
                PropertyInfo propInfo = null;
                do
                {
                    propInfo = type.GetProperty(propertyName,
                           BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    type = type.BaseType;
                }
                while (propInfo == null && type != null);
                return propInfo;
            }
        }
    }
}
