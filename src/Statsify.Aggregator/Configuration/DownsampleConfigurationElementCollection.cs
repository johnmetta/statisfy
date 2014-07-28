using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace Statsify.Aggregator.Configuration
{
    [ConfigurationCollection(typeof(DownsampleConfigurationElement), CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class DownsampleConfigurationElementCollection : ConfigurationElementCollection, IEnumerable<DownsampleConfigurationElement>
    {
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
            return new DownsampleConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((DownsampleConfigurationElement)element).Name;
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        protected override string ElementName
        {
            get { return "downsample"; }
        }

        IEnumerator<DownsampleConfigurationElement> IEnumerable<DownsampleConfigurationElement>.GetEnumerator()
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var value in ((IEnumerable)this))
            {
                var element = (DownsampleConfigurationElement)value;
                if (element != null)
                    yield return element;
            }           
        }
    }
}