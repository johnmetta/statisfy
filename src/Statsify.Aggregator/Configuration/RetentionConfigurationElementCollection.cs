using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Statsify.Core.Storage;

namespace Statsify.Aggregator.Configuration
{
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