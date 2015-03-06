using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Statsify.Core.Storage;

namespace Statsify.Aggregator.Configuration
{
    public class StoreConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("pattern", IsRequired = true)]
        public string Pattern
        {
            get { return (string)this["pattern"]; }
            set { this["pattern"] = value; }
        }

        [ConfigurationProperty("retentions", IsRequired = true)]
        public RetentionConfigurationElementCollection Retentions
        {
            get { return (RetentionConfigurationElementCollection)this["retentions"]; }
            set { this["retentions"] = value; }
        }
    }

    public class RetentionConfigurationElement : ConfigurationElement, IRetentionDefinition
    {
        [ConfigurationProperty("precision", IsRequired = true)]
        public TimeSpan Precision
        {
            get { return (TimeSpan)this["precision"]; }
            set { this["precision"] = value; }
        }

        [ConfigurationProperty("history", IsRequired = true)]
        public TimeSpan History
        {
            get { return (TimeSpan)this["history"]; }
            set { this["history"] = value; }
        }

    }

    public class RetentionConfigurationElementCollection : ConfigurationElementCollection, IEnumerable<RetentionConfigurationElement>, IEnumerable<IRetentionDefinition>
    {
        public RetentionConfigurationElement this[int index]
        {
            get { return (RetentionConfigurationElement)BaseGet(index); }
            set
            {
                if(BaseGet(index) != null)
                    BaseRemoveAt(index);

                base.BaseAdd(index, value);
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new RetentionConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return element;
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        protected override string ElementName
        {
            get { return "retention"; }
        }

        IEnumerator<IRetentionDefinition> IEnumerable<IRetentionDefinition>.GetEnumerator()
        {
            return this.OfType<IRetentionDefinition>().GetEnumerator();
        }

        IEnumerator<RetentionConfigurationElement> IEnumerable<RetentionConfigurationElement>.GetEnumerator()
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var value in ((IEnumerable)this))
            {
                var element = (RetentionConfigurationElement)value;
                if (element != null)
                    yield return element;
            }           
        }
    }
}