using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace Statsify.Aggregator.Configuration
{
    [ConfigurationCollection(typeof(StorageConfigurationElement), CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class StorageConfigurationElementCollection : ConfigurationElementCollection, IEnumerable<StorageConfigurationElement>
    {
        [ConfigurationProperty("path", IsRequired = true)]
        public string Path
        {
            get { return (string)this["path"]; }
            set { this["path"] = value; }
        }

        [ConfigurationProperty("flush-interval", IsRequired = true)]
        public TimeSpan FlushInterval
        {
            get { return (TimeSpan)this["flush-interval"]; }
            set { this["flush-interval"] = value; }
        }

        public StorageConfigurationElement this[int index]
        {
            get { return (StorageConfigurationElement)BaseGet(index); }
            set
            {
                if(BaseGet(index) != null)
                    BaseRemoveAt(index);

                base.BaseAdd(index, value);
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new StorageConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((StorageConfigurationElement)element).Name;
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        protected override string ElementName
        {
            get { return "store"; }
        }

        IEnumerator<StorageConfigurationElement> IEnumerable<StorageConfigurationElement>.GetEnumerator()
        {
            foreach(var value in ((IEnumerable)this))
            {
                var element = (StorageConfigurationElement)value;
                if(element != null)
                    yield return element;
            } // foreach
        }
    }
}